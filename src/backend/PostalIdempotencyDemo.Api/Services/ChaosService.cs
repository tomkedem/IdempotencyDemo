using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Models.DTO;
using PostalIdempotencyDemo.Api.Repositories;


namespace PostalIdempotencyDemo.Api.Services
{
    using PostalIdempotencyDemo.Api.Services.Interfaces;

    public class ChaosService : IChaosService
    {
        private readonly ISettingsRepository _settingsRepository;

        public ChaosService(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<ChaosSettingsDto> GetChaosSettingsAsync()
        {
            var settings = await _settingsRepository.GetSettingsAsync();
            var settingsDict = settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            return new ChaosSettingsDto
            {
                UseIdempotencyKey = bool.TryParse(settingsDict.GetValueOrDefault("UseIdempotencyKey"), out var use) && use,
              
                IdempotencyExpirationHours = int.TryParse(settingsDict.GetValueOrDefault("IdempotencyExpirationHours"), out var hours) ? hours : 24,
                
             };
        }

        public async Task<bool> UpdateChaosSettingsAsync(ChaosSettingsDto settingsDto)
        {
            var settings = new List<SystemSetting>
            {
                new() { SettingKey = "UseIdempotencyKey", SettingValue = settingsDto.UseIdempotencyKey.ToString().ToLower() },
                new() { SettingKey = "IdempotencyExpirationHours", SettingValue = settingsDto.IdempotencyExpirationHours.ToString() },                
            };

            return await _settingsRepository.UpdateSettingsAsync(settings);
        }       

    }
}
