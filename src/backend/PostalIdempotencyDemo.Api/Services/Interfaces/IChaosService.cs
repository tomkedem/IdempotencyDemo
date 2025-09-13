using PostalIdempotencyDemo.Api.Models.DTO;

namespace PostalIdempotencyDemo.Api.Services.Interfaces;

public interface IChaosService
{
    Task<ChaosSettingsDto> GetChaosSettingsAsync();
    Task<bool> UpdateChaosSettingsAsync(ChaosSettingsDto settingsDto);
        
}
