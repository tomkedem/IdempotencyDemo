using PostalIdempotencyDemo.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http; // Added the missing using statement

namespace PostalIdempotencyDemo.Api.Services
{
    public class HttpClientService(HttpClient httpClient, IChaosService chaosService, ILogger<HttpClientService> logger) : IHttpClientService
    {
        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            await ConfigureTimeoutAsync();
            logger.LogDebug("Making GET request to {RequestUri} with timeout {Timeout}s",
                requestUri, httpClient.Timeout.TotalSeconds);

            return await httpClient.GetAsync(requestUri);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            await ConfigureTimeoutAsync();
            logger.LogDebug("Making POST request to {RequestUri} with timeout {Timeout}s",
                requestUri, httpClient.Timeout.TotalSeconds);

            return await httpClient.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            await ConfigureTimeoutAsync();
            logger.LogDebug("Making PUT request to {RequestUri} with timeout {Timeout}s",
                requestUri, httpClient.Timeout.TotalSeconds);

            return await httpClient.PutAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            await ConfigureTimeoutAsync();
            logger.LogDebug("Making DELETE request to {RequestUri} with timeout {Timeout}s",
                requestUri, httpClient.Timeout.TotalSeconds);

            return await httpClient.DeleteAsync(requestUri);
        }

        public async Task<string> GetStringAsync(string requestUri)
        {
            await ConfigureTimeoutAsync();
            logger.LogDebug("Making GET string request to {RequestUri} with timeout {Timeout}s",
                requestUri, httpClient.Timeout.TotalSeconds);

            return await httpClient.GetStringAsync(requestUri);
        }

        public async Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content)
        {
            await ConfigureTimeoutAsync();
            logger.LogDebug("Making PATCH request to {RequestUri} with timeout {Timeout}s",
                requestUri, httpClient.Timeout.TotalSeconds);

            return await httpClient.PatchAsync(requestUri, content);
        }

        private async Task ConfigureTimeoutAsync()
        {
            // Using fixed timeout since chaos settings functionality was removed
            var timeoutSeconds = 30; // Default to 30 seconds
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            await Task.CompletedTask; // Keep async signature for consistency
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
