using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Repositories;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Services
{
    public class IdempotencyService(IIdempotencyRepository repository, ILogger<IdempotencyService> logger, ISettingsRepository settingsRepository) : IIdempotencyService
    {
        public string GenerateIdempotencyKey(string requestContent)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(requestContent));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public async Task<IdempotencyEntry?> GetIdempotencyEntryAsync(string idempotencyKey)
        {
            return await repository.GetByKeyAsync(idempotencyKey);
        }

        public async Task<bool> StoreIdempotencyEntryAsync(IdempotencyEntry entry)
        {
            var success = await repository.CreateAsync(entry);
            if (success)
            {
                logger.LogInformation("Saved idempotency entry for key {IdempotencyKey}", entry.IdempotencyKey);
            }
            else
            {
                logger.LogError("Failed to save idempotency entry for key {IdempotencyKey}", entry.IdempotencyKey);
            }
            return success;
        }

        public async Task CleanupExpiredEntriesAsync()
        {
            var success = await repository.DeleteExpiredAsync();
            if (success)
            {
                logger.LogInformation("Cleaned up expired idempotency entries");
            }
        }



        public async Task CacheResponseAsync(string idempotencyKey, object response)
        {
            IEnumerable<SystemSetting> settings = await settingsRepository.GetSettingsAsync();
            SystemSetting? useIdempotencySetting = settings.FirstOrDefault(s => s.SettingKey == "UseIdempotencyKey");
            if (idempotencyKey == null || useIdempotencySetting?.SettingValue != "true") return; // Do not cache if disabled

            var responseData = JsonSerializer.Serialize(response);
            // StatusCode can be set as needed, here using 200 as default
            await repository.UpdateResponseAsync(idempotencyKey, responseData, 200);
        }

        public async Task<IdempotencyEntry?> GetLatestEntryByRequestPathAsync(string requestPath)
        {
            return await repository.GetLatestByRequestPathAsync(requestPath);
        }
    }
}
