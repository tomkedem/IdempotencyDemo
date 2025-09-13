using PostalIdempotencyDemo.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PostalIdempotencyDemo.Api.Services
{
    public class RetryService : IRetryService
    {
        private readonly IChaosService _chaosService;
        private readonly ILogger<RetryService> _logger;

        public RetryService(IChaosService chaosService, ILogger<RetryService> logger)
        {
            _chaosService = chaosService;
            _logger = logger;
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            // Using fixed retry count since chaos settings functionality was removed
            var maxRetries = 3; // Default to 3 retries
            var attempt = 0;
            Exception? lastException = null;

            while (attempt <= maxRetries)
            {
                try
                {
                    _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxRetries}",
                        operationName, attempt + 1, maxRetries + 1);

                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt > maxRetries)
                    {
                        _logger.LogError(ex, "Operation {OperationName} failed after {MaxRetries} attempts",
                            operationName, maxRetries + 1);
                        break;
                    }

                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning("Operation {OperationName} failed on attempt {Attempt}, retrying in {Delay}ms. Error: {Error}",
                        operationName, attempt, delay, ex.Message);

                    await Task.Delay(delay);
                }
            }

            throw new InvalidOperationException(
                $"Operation '{operationName}' failed after {maxRetries + 1} attempts",
                lastException);
        }

        public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true; // Return dummy value for void operations
            }, operationName);
        }

        private static int CalculateDelay(int attempt)
        {
            // Exponential backoff: 100ms, 200ms, 400ms, 800ms, etc.
            return (int)(100 * Math.Pow(2, attempt - 1));
        }
    }
}
