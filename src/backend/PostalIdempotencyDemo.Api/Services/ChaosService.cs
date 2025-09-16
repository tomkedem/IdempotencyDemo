using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Models.DTO;
using PostalIdempotencyDemo.Api.Repositories;


namespace PostalIdempotencyDemo.Api.Services
{
    using PostalIdempotencyDemo.Api.Services.Interfaces;

    public class ChaosService(ISettingsRepository settingsRepository) : IChaosService
    {
        public async Task<ChaosSettingsDto> GetChaosSettingsAsync()
        {
            var settings = await settingsRepository.GetSettingsAsync();
            var settingsDict = settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            return new ChaosSettingsDto
            {
                UseIdempotencyKey = bool.TryParse(settingsDict.GetValueOrDefault("UseIdempotencyKey"), out bool use) && use,
              
                IdempotencyExpirationHours = int.TryParse(settingsDict.GetValueOrDefault("IdempotencyExpirationHours"), out int hours) ? hours : 24,
            };
        }

        public async Task<bool> UpdateChaosSettingsAsync(ChaosSettingsDto settingsDto)
        {
            var settings = new List<SystemSetting>
            {
                new() { SettingKey = "UseIdempotencyKey", SettingValue = settingsDto.UseIdempotencyKey.ToString().ToLower() },
                new() { SettingKey = "IdempotencyExpirationHours", SettingValue = settingsDto.IdempotencyExpirationHours.ToString() },                
            };

            return await settingsRepository.UpdateSettingsAsync(settings);
        }       

    }
}
