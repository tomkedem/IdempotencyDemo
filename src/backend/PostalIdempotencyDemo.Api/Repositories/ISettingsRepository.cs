using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Repositories;

public interface ISettingsRepository
{
    Task<IEnumerable<SystemSetting>> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(IEnumerable<SystemSetting> settings);
}
