using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Repositories;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Services
{
    /// <summary>
    /// שירות ארגון הגנה אידמפוטנטית - מתאם בין שירותי האידמפוטנטיות והעסקיים
    /// </summary>
    public class IdempotencyOrchestrationService : IIdempotencyOrchestrationService
    {
        private readonly IIdempotencyService _idempotencyService;
        private readonly IDeliveryService _deliveryService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMetricsService _metricsService;
        private readonly IMetricsRepository _metricsRepository;
        private readonly ILogger<IdempotencyOrchestrationService> _logger;

        public IdempotencyOrchestrationService(
            IIdempotencyService idempotencyService,
            IDeliveryService deliveryService,
            ISettingsRepository settingsRepository,
            IMetricsService metricsService,
            IMetricsRepository metricsRepository,
            ILogger<IdempotencyOrchestrationService> logger)
        {
            _idempotencyService = idempotencyService;
            _deliveryService = deliveryService;
            _settingsRepository = settingsRepository;
            _metricsService = metricsService;
            _metricsRepository = metricsRepository;
            _logger = logger;
        }

        /// <summary>
        /// עיבוד יצירת משלוח עם הגנה אידמפוטנטית מלאה
        /// </summary>
        public async Task<IdempotencyDemoResponse<Delivery>> ProcessCreateDeliveryWithIdempotencyAsync(
            CreateDeliveryRequest request,
            string idempotencyKey,
            string requestPath)
        {
            _logger.LogInformation("מעבד יצירת משלוח עם הגנה אידמפוטנטית. מפתח: {IdempotencyKey}", idempotencyKey);

            // שלב 0: בדיקה אם הגנת אידמפוטנטיות מופעלת
            var isIdempotencyEnabled = await IsIdempotencyEnabledAsync();
            if (!isIdempotencyEnabled)
            {
                _logger.LogInformation("הגנת אידמפוטנטיות כבויה - בודק אם זו פעולה כפולה לצורכי תיעוד");

                // בדיקה אם זו פעולה כפולה גם כשההגנה כבויה
                var existingEntry = await _idempotencyService.GetLatestEntryByRequestPathAsync(requestPath);

                bool isDuplicateOperation = existingEntry != null && existingEntry.IdempotencyKey == idempotencyKey;

                // בכל מקרה מבצעים את הפעולה (גם אם זו כפילות)
                _logger.LogInformation("הגנת אידמפוטנטיות כבויה - מעבד בקשה ישירות ללא הגנה");
                var directResponse = await _deliveryService.CreateDeliveryAsync(request);

                if (isDuplicateOperation)
                {
                    // זוהתה פעולה כפולה - מתעדים כשגיאה אבל מאפשרים את הפעולה
                    _logger.LogWarning("זוהתה פעולה כפולה כאשר הגנת אידמפוטנטיות כבויה - מתעד כשגיאה אבל מאפשר פעולה. מפתח: {IdempotencyKey}", idempotencyKey);

                    // תיעוד כשגיאה בטבלת operation_metrics עם is_error = 1
                    await LogChaosDisabledErrorForCreateAsync(idempotencyKey, requestPath, "duplicate_operation_without_protection");
                }
                else
                {
                    // פעולה ראשונה - יצירת רשומה למעקב רגילה
                    await CreateTrackingEntryForCreateAsync(request, idempotencyKey, requestPath);
                }

                return directResponse;
            }

            // שלב 1: בדיקה אם קיימת רשומה אידמפוטנטית קודמת
            var latestEntry = await _idempotencyService.GetLatestEntryByRequestPathAsync(requestPath);

            if (latestEntry != null && latestEntry.IdempotencyKey == idempotencyKey && latestEntry.ExpiresAt > DateTime.Now)
            {
                // נמצאה רשומה תקפה עם אותו מפתח - זו בקשה כפולה
                _logger.LogInformation("נמצאה בקשה כפולה - מחזיר תשובה שמורה. מפתח: {IdempotencyKey}", idempotencyKey);

                if (latestEntry.ResponseData != null)
                {
                    var cachedResponse = JsonSerializer.Deserialize<IdempotencyDemoResponse<Delivery>>(latestEntry.ResponseData);
                    return cachedResponse ?? new IdempotencyDemoResponse<Delivery> { Success = false, Message = "שגיאה בקריאת תשובה שמורה" };
                }
            }

            // שלב 2: יצירת רשומה אידמפוטנטית חדשה
            _logger.LogDebug("יוצר רשומה אידמפוטנטית חדשה");
            var expirationHours = await GetIdempotencyExpirationHoursAsync();

            var newEntry = new IdempotencyEntry
            {
                Id = Guid.NewGuid().ToString(),
                IdempotencyKey = idempotencyKey,
                RequestHash = ComputeSha256Hash(JsonSerializer.Serialize(request)),
                Endpoint = requestPath,
                HttpMethod = "POST",
                StatusCode = 0,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(expirationHours),
                Operation = "create_delivery",
                CorrelationId = requestPath,
                RelatedEntityId = null
            };
            await _idempotencyService.StoreIdempotencyEntryAsync(newEntry);

            // שלב 3: עיבוד הבקשה בפועל
            _logger.LogInformation("מעבד בקשה חדשה ליצירת משלוח");
            var response = await _deliveryService.CreateDeliveryAsync(request);

            // שלב 4: שמירת התשובה למקרה של בקשות כפולות עתידיות
            await _idempotencyService.CacheResponseAsync(idempotencyKey, response);

            _logger.LogInformation("משלוח נוצר בהצלחה עם מפתח אידמפוטנטיות: {IdempotencyKey}", idempotencyKey);
            return response;
        }

        /// <summary>
        /// עיבוד עדכון סטטוס משלוח עם הגנה אידמפוטנטית מלאה
        /// </summary>
        public async Task<IdempotencyDemoResponse<Shipment>> ProcessUpdateDeliveryStatusWithIdempotencyAsync(
            string barcode,
            UpdateDeliveryStatusRequest request,
            string idempotencyKey,
            string requestPath
            )
        {
            _logger.LogInformation("מעבד עדכון סטטוס משלוח {Barcode} עם הגנה אידמפוטנטית. מפתח: {IdempotencyKey}", requestPath, idempotencyKey);

            // שלב 0: בדיקה אם הגנת אידמפוטנטיות מופעלת
            bool isIdempotencyEnabled = await IsIdempotencyEnabledAsync();
            if (isIdempotencyEnabled)
            {
                _logger.LogDebug("מחפש רשומה אידמפוטנטית קיימת עבור request_path: {requestPath}", requestPath);

                IdempotencyEntry? latestEntry = await _idempotencyService.GetLatestEntryByRequestPathAsync(requestPath);

                if (latestEntry != null && latestEntry.IdempotencyKey == idempotencyKey && latestEntry.ExpiresAt > DateTime.Now)
                {
                    // זו בקשה כפולה - חוסמים ומתעדים בלוג
                    _logger.LogWarning("בקשה כפולה לעדכון סטטוס זוהתה וחסומה. ברקוד: {Barcode}, מפתח: {IdempotencyKey}", barcode, idempotencyKey);

                    if (latestEntry.ResponseData != null)
                    {
                        // תיעוד hit אידמפוטנטי בטבלת operation_metrics
                        await _deliveryService.LogIdempotentHitAsync(barcode, idempotencyKey, requestPath);

                        // החזרת הודעה עקבית על חסימה
                        return new IdempotencyDemoResponse<Shipment>
                        {
                            Success = true,
                            Data = null,
                            Message = "העדכון נחסם בגלל מפתח אידמפונטנטי, סטטוס לא שונה."
                        };
                    }
                }

                // יצירת רשומה אידמפוטנטית חדשה
                _logger.LogDebug("יוצר רשומה אידמפוטנטית חדשה לעדכון סטטוס");
                var expirationHours = await GetIdempotencyExpirationHoursAsync();

                var newEntry = new IdempotencyEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    IdempotencyKey = idempotencyKey,
                    RequestHash = ComputeSha256Hash(JsonSerializer.Serialize(request)),
                    Endpoint = requestPath,
                    HttpMethod = "PATCH",
                    StatusCode = 0,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(expirationHours),
                    Operation = "update_status",
                    RelatedEntityId = barcode
                };
                await _idempotencyService.StoreIdempotencyEntryAsync(newEntry);

                // עדכון הסטטוס בפועל
                _logger.LogInformation("מעדכן סטטוס משלוח {Barcode} לסטטוס {StatusId}", barcode, request.StatusId);
                var response = await _deliveryService.UpdateDeliveryStatusAsync("update_status", barcode, request.StatusId, requestPath);

                // שמירת התשובה למקרה של בקשות כפולות עתידיות
                await _idempotencyService.CacheResponseAsync(idempotencyKey, response);

                if (!response.Success)
                {
                    _logger.LogWarning("עדכון סטטוס נכשל עבור ברקוד {Barcode}: {Message}", barcode, response.Message);
                }
                else
                {
                    _logger.LogInformation("סטטוס משלוח {Barcode} עודכן בהצלחה", barcode);
                }
                return response;
            }
            else
            {
                _logger.LogInformation("הגנת אידמפוטנטיות כבויה - בודק אם זו פעולה כפולה לצורכי תיעוד");

                // בדיקה אם זו פעולה כפולה גם כשההגנה כבויה - מחפש לפי ברקוד
                string correlationIdForCheck = requestPath;
                IdempotencyEntry? existingEntry = await _idempotencyService.GetLatestEntryByRequestPathAsync(correlationIdForCheck);

                bool isDuplicateOperation = existingEntry != null && existingEntry.IdempotencyKey == idempotencyKey;

                if (isDuplicateOperation)
                {
                    // זוהתה פעולה כפולה - מתעדים כשגיאה אבל מאפשרים את הפעולה
                    _logger.LogWarning("זוהתה פעולה כפולה כאשר הגנת אידמפוטנטיות כבויה - מתעד כשגיאה אבל מאפשר פעולה. ברקוד: {Barcode}", barcode);
                    // עדכון הסטטוס בפועל
                    _logger.LogInformation("מעדכן סטטוס משלוח {Barcode} לסטטוס {StatusId}", barcode, request.StatusId);
                    IdempotencyDemoResponse<Shipment> duplicateResponse = await _deliveryService.UpdateDeliveryStatusAsync("update_status_Idempotency_disabled",barcode, request.StatusId, requestPath);
                     await CreateTrackingEntryAsync(barcode, request, idempotencyKey, requestPath);
                    return duplicateResponse;
                }
                else
                {
                    // עדכון הסטטוס בפועל
                    _logger.LogInformation("מעדכן סטטוס משלוח {Barcode} לסטטוס {StatusId}", barcode, request.StatusId);
                    IdempotencyDemoResponse<Shipment> directResponse = await _deliveryService.UpdateDeliveryStatusAsync("update_status_Idempotency_disabled_F",barcode, request.StatusId, requestPath);

                    // פעולה ראשונה - יצירת רשומה למעקב לפני הביצוע
                    await CreateTrackingEntryAsync(barcode, request, idempotencyKey, requestPath);

                    // ביצוע הפעולה עם תיעוד רגיל
                    _logger.LogInformation("הגנת אידמפוטנטיות כבויה - מעבד בקשה ישירות ללא הגנה");
                   // var directResponse = await _deliveryService.UpdateDeliveryStatusAsync(barcode, request.StatusId, requestPath);

                    return directResponse;
                }
            }
            //return response;
        }

        /// <summary>
        /// בדיקה אם הגנת אידמפוטנטיות מופעלת בהגדרות המערכת
        /// </summary>
        private async Task<bool> IsIdempotencyEnabledAsync()
        {
            try
            {
                _logger.LogDebug("בודק אם הגנת אידמפוטנטיות מופעלת");

                var settings = await _settingsRepository.GetSettingsAsync();
                var idempotencyEnabledSetting = settings.FirstOrDefault(s => s.SettingKey == "UseIdempotencyKey");

                if (idempotencyEnabledSetting != null && bool.TryParse(idempotencyEnabledSetting.SettingValue, out bool isEnabled))
                {
                    _logger.LogDebug("מצב הגנת אידמפוטנטיות: {IsEnabled}", isEnabled ? "מופעל" : "כבוי");
                    return isEnabled;
                }

                // ברירת מחדל - הגנה מופעלת
                _logger.LogWarning("לא נמצאה הגדרת UseIdempotencyKey, משתמש בברירת מחדל: מופעל");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה בבדיקת מצב הגנת אידמפוטנטיות, משתמש בברירת מחדל: מופעל");
                return true; // ברירת מחדל בטוחה - הגנה מופעלת
            }
        }


        /// <summary>
        /// קריאת זמן תפוגה אידמפוטנטיות מטבלת SystemSettings
        /// </summary>
        private async Task<int> GetIdempotencyExpirationHoursAsync()
        {
            _logger.LogDebug("קורא זמן תפוגה אידמפוטנטיות מטבלת ההגדרות");

            var settings = await _settingsRepository.GetSettingsAsync();
            var expirationSetting = settings.FirstOrDefault(s => s.SettingKey == "IdempotencyExpirationHours");

            if (expirationSetting != null && int.TryParse(expirationSetting.SettingValue, out int hours) && hours > 0)
            {
                _logger.LogDebug("זמן תפוגה אידמפוטנטיות נקרא מההגדרות: {Hours} שעות", hours);
                return hours;
            }

            _logger.LogWarning("לא נמצא זמן תפוגה תקין בהגדרות, משתמש בברירת מחדל: 24 שעות");
            return 24;
        }

        /// <summary>
        /// חישוב hash SHA256 עבור תוכן הבקשה
        /// </summary>
        private string ComputeSha256Hash(string input)
        {
            _logger.LogDebug("מחשב SHA256 hash עבור תוכן בקשה");
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var result = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            _logger.LogDebug("SHA256 hash חושב: {HashPrefix}...", result[..8]);
            return result;
        }

        /// <summary>
        /// תיעוד שגיאה שנוצרה כאשר הגנת הכאוס הייתה כבויה
        /// </summary>
        private async Task LogChaosDisabledErrorAsync(string barcode, string idempotencyKey, string requestPath, string errorType)
        {
            try
            {
                _logger.LogInformation("מתעד שגיאה שנוצרה בגלל הגנת כאוס כבויה: {ErrorType}", errorType);

                // תיעוד שגיאה ישירות בשירות המטריקות
                _metricsService.RecordChaosDisabledError($"update_status_chaos_error");

                // תיעוד שגיאה ב-operation_metrics עם is_error = 1
                await _metricsRepository.LogMetricsAsync(
                    operationType: $"update_status_chaos_error",
                    endpoint: requestPath,
                    executionTimeMs: 0,
                    isIdempotentHit: false,
                    idempotencyKey: idempotencyKey,
                    isError: true // מסמן כשגיאה בבסיס הנתונים
                );

                _logger.LogDebug("שגיאת הגנת כאוס כבויה תועדה בהצלחה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה בתיעוד שגיאת הגנת כאוס כבויה");
            }
        }

        /// <summary>
        /// יצירת רשומה למעקב (אבל לא לחסימה) כאשר הגנה כבויה
        /// </summary>
        private async Task CreateTrackingEntryAsync(string barcode, UpdateDeliveryStatusRequest request, string idempotencyKey, string requestPath)
        {
            try
            {
                var expirationHours = await GetIdempotencyExpirationHoursAsync();

                var trackingEntry = new IdempotencyEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    IdempotencyKey = idempotencyKey,
                    RequestHash = ComputeSha256Hash(JsonSerializer.Serialize(request)),
                    Endpoint = requestPath,
                    HttpMethod = "PATCH",
                    StatusCode = 200, // הצליח אבל ללא הגנה
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(expirationHours),
                    Operation = "update_status_unprotected",
                    RelatedEntityId = barcode
                };

                await _idempotencyService.StoreIdempotencyEntryAsync(trackingEntry);
                _logger.LogDebug("נוצרה רשומת מעקב לפעולה ללא הגנה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה ביצירת רשומת מעקב");
            }
        }

        /// <summary>
        /// תיעוד שגיאה שנוצרה כאשר הגנת הכאוס הייתה כבויה - עבור יצירת משלוח
        /// </summary>
        private async Task LogChaosDisabledErrorForCreateAsync(string idempotencyKey, string requestPath, string errorType)
        {
            try
            {
                _logger.LogInformation("מתעד שגיאה שנוצרה בגלל הגנת כאוס כבויה ביצירת משלוח: {ErrorType}", errorType);

                // תיעוד שגיאה ישירות בשירות המטריקות
                _metricsService.RecordChaosDisabledError($"create_delivery_chaos_error");

                // תיעוד שגיאה ב-operation_metrics עם is_error = 1
                await _metricsRepository.LogMetricsAsync(
                    operationType: $"create_delivery_chaos_error",
                    endpoint: requestPath,
                    executionTimeMs: 0,
                    isIdempotentHit: false,
                    idempotencyKey: idempotencyKey,
                    isError: true // מסמן כשגיאה בבסיס הנתונים
                );

                _logger.LogDebug("שגיאת הגנת כאוס כבויה תועדה בהצלחה ליצירת משלוח");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה בתיעוד שגיאת הגנת כאוס כבויה ליצירת משלוח");
            }
        }

        /// <summary>
        /// יצירת רשומה למעקב (אבל לא לחסימה) כאשר הגנה כבויה - עבור יצירת משלוח
        /// </summary>
        private async Task CreateTrackingEntryForCreateAsync(CreateDeliveryRequest request, string idempotencyKey, string requestPath)
        {
            try
            {
                var expirationHours = await GetIdempotencyExpirationHoursAsync();

                var trackingEntry = new IdempotencyEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    IdempotencyKey = idempotencyKey,
                    RequestHash = ComputeSha256Hash(JsonSerializer.Serialize(request)),
                    Endpoint = requestPath,
                    HttpMethod = "POST",
                    StatusCode = 200, // הצליח אבל ללא הגנה
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(expirationHours),
                    Operation = "create_delivery_unprotected",
                    RelatedEntityId = null
                };

                await _idempotencyService.StoreIdempotencyEntryAsync(trackingEntry);
                _logger.LogDebug("נוצרה רשומת מעקב ליצירת משלוח ללא הגנה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה ביצירת רשומת מעקב ליצירת משלוח");
            }
        }
    }
}
