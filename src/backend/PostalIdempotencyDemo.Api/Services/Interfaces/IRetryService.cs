namespace PostalIdempotencyDemo.Api.Services.Interfaces
{
    public interface IRetryService
    {
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName);
        Task ExecuteWithRetryAsync(Func<Task> operation, string operationName);
    }
}
