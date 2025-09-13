using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Services.Interfaces
{
    public interface IIdempotencyService
    {
        string GenerateIdempotencyKey(string requestContent);
        Task<IdempotencyEntry?> GetIdempotencyEntryAsync(string idempotencyKey);
        Task<bool> StoreIdempotencyEntryAsync(IdempotencyEntry entry);
        Task CleanupExpiredEntriesAsync();
        Task CacheResponseAsync(string idempotencyKey, object response);
        Task<IdempotencyEntry?> GetLatestEntryByRequestPathAsync(string requestPath);
    }
}
