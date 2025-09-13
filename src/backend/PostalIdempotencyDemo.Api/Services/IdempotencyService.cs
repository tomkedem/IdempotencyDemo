using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Repositories;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Services
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly IIdempotencyRepository _repository;
        private readonly ILogger<IdempotencyService> _logger;
        private readonly ISettingsRepository _settingsRepository;

        public IdempotencyService(IIdempotencyRepository repository, ILogger<IdempotencyService> logger, ISettingsRepository settingsRepository)
        {
            _repository = repository;
            _logger = logger;
            _settingsRepository = settingsRepository;
        }

        public string GenerateIdempotencyKey(string requestContent)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(requestContent));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public async Task<IdempotencyEntry?> GetIdempotencyEntryAsync(string idempotencyKey)
        {
            return await _repository.GetByKeyAsync(idempotencyKey);
        }

        public async Task<bool> StoreIdempotencyEntryAsync(IdempotencyEntry entry)
        {
            var success = await _repository.CreateAsync(entry);
            if (success)
            {
                _logger.LogInformation("Saved idempotency entry for key {IdempotencyKey}", entry.IdempotencyKey);
            }
            else
            {
                _logger.LogError("Failed to save idempotency entry for key {IdempotencyKey}", entry.IdempotencyKey);
            }
            return success;
        }

        public async Task CleanupExpiredEntriesAsync()
        {
            var success = await _repository.DeleteExpiredAsync();
            if (success)
            {
                _logger.LogInformation("Cleaned up expired idempotency entries");
            }
        }



        public async Task CacheResponseAsync(string idempotencyKey, object response)
        {
            IEnumerable<SystemSetting> settings = await _settingsRepository.GetSettingsAsync();
            SystemSetting? useIdempotencySetting = settings.FirstOrDefault(s => s.SettingKey == "UseIdempotencyKey");
            if (idempotencyKey == null || useIdempotencySetting?.SettingValue != "true") return; // Do not cache if disabled

            var responseData = JsonSerializer.Serialize(response);
            // StatusCode can be set as needed, here using 200 as default
            await _repository.UpdateResponseAsync(idempotencyKey, responseData, 200);
        }

        public async Task<IdempotencyEntry?> GetLatestEntryByRequestPathAsync(string requestPath)
        {
            return await _repository.GetLatestByRequestPathAsync(requestPath);
        }
    }
}
