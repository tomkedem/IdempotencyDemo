using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Repositories;

namespace PostalIdempotencyDemo.Api.Services
{
    /// <summary>
    /// Service for handling complete data cleanup operations with safety measures
    /// Follows Single Responsibility and Dependency Inversion principles
    /// </summary>
    public class DataCleanupService : IDataCleanupService
    {
        private readonly IDataCleanupRepository _cleanupRepository;
        private readonly IMetricsService _metricsService;
        private readonly ILogger<DataCleanupService> _logger;

        // Thread-safe storage for confirmation tokens with expiration
        private static readonly ConcurrentDictionary<string, DateTime> _confirmationTokens = new();
        private const int TOKEN_EXPIRY_MINUTES = 5; // Token expires after 5 minutes

        public DataCleanupService(
            IDataCleanupRepository cleanupRepository,
            IMetricsService metricsService,
            ILogger<DataCleanupService> logger)
        {
            _cleanupRepository = cleanupRepository ?? throw new ArgumentNullException(nameof(cleanupRepository));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateConfirmationToken()
        {
            // Clean expired tokens first
            CleanExpiredTokens();

            // Generate secure random token
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            var token = Convert.ToBase64String(tokenBytes);
            var expiryTime = DateTime.Now.AddMinutes(TOKEN_EXPIRY_MINUTES);

            _confirmationTokens[token] = expiryTime;

            _logger.LogWarning("Cleanup confirmation token generated, expires at {ExpiryTime}", expiryTime);
            return token;
        }

        public bool ValidateConfirmationToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (!_confirmationTokens.TryGetValue(token, out var expiryTime))
                return false;

            if (DateTime.Now > expiryTime)
            {
                _confirmationTokens.TryRemove(token, out _);
                return false;
            }

            return true;
        }

        public async Task<ServiceResult<CleanupPreview>> GetCleanupPreviewAsync()
        {
            try
            {
                var preview = new CleanupPreview
                {
                    IdempotencyEntriesCount = await _cleanupRepository.GetIdempotencyEntriesCountAsync(),
                    OperationMetricsCount = await _cleanupRepository.GetOperationMetricsCountAsync(),
                    OldestIdempotencyEntry = await _cleanupRepository.GetOldestIdempotencyEntryDateAsync() ?? DateTime.Now,
                    OldestMetricEntry = await _cleanupRepository.GetOldestMetricsEntryDateAsync() ?? DateTime.Now
                };

                // Rough estimate of data size (not exact, but gives an idea)
                preview.EstimatedDataSizeKB = (long)((preview.IdempotencyEntriesCount * 1.0) + (preview.OperationMetricsCount * 0.5)); // Rough KB estimate

                _logger.LogInformation("Cleanup preview generated: {IdempotencyCount} idempotency entries, {MetricsCount} metrics entries",
                    preview.IdempotencyEntriesCount, preview.OperationMetricsCount);

                return ServiceResult<CleanupPreview>.Success(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate cleanup preview");
                return ServiceResult<CleanupPreview>.Failure($"Failed to generate cleanup preview: {ex.Message}");
            }
        }

        public async Task<ServiceResult<string>> PerformCompleteCleanupAsync(string confirmationToken)
        {
            try
            {
                // Validate confirmation token
                if (!ValidateConfirmationToken(confirmationToken))
                {
                    _logger.LogWarning("Invalid or expired confirmation token provided for cleanup");
                    return ServiceResult<string>.Failure("Invalid or expired confirmation token");
                }

                // Get preview data for logging
                var previewResult = await GetCleanupPreviewAsync();
                if (!previewResult.IsSuccess)
                {
                    return ServiceResult<string>.Failure("Failed to get cleanup preview");
                }

                var preview = previewResult.Data!;

                _logger.LogCritical("STARTING COMPLETE DATABASE CLEANUP - This will delete {IdempotencyCount} idempotency entries and {MetricsCount} metrics entries",
                    preview.IdempotencyEntriesCount, preview.OperationMetricsCount);

                // Perform the actual cleanup
                var success = await _cleanupRepository.PerformCompleteCleanupAsync();

                if (success)
                {
                    // Reset in-memory metrics as well
                    _metricsService.ResetMetrics();

                    // Remove the used token
                    _confirmationTokens.TryRemove(confirmationToken, out _);

                    var message = $"Complete cleanup successful! Deleted {preview.IdempotencyEntriesCount} idempotency entries and {preview.OperationMetricsCount} metrics entries.";
                    _logger.LogCritical("COMPLETE DATABASE CLEANUP SUCCESSFUL");

                    return ServiceResult<string>.Success(message);
                }
                else
                {
                    _logger.LogError("Database cleanup failed");
                    return ServiceResult<string>.Failure("Database cleanup failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during complete cleanup");
                return ServiceResult<string>.Failure($"Critical error during cleanup: {ex.Message}");
            }
        }

        private void CleanExpiredTokens()
        {
            var now = DateTime.Now;
            var expiredTokens = _confirmationTokens
                .Where(kvp => now > kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expiredToken in expiredTokens)
            {
                _confirmationTokens.TryRemove(expiredToken, out _);
            }
        }
    }
}
