using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Repositories
{
    public interface IIdempotencyRepository
    {
        Task<IdempotencyEntry?> GetLatestByRequestPathAsync(string requestPath);
        Task<IdempotencyEntry?> GetByKeyAsync(string key);
        Task<bool> CreateAsync(IdempotencyEntry entry);
        Task<bool> UpdateResponseAsync(string key, string responseData, int statusCode);
        Task<bool> DeleteExpiredAsync();
    }
}
