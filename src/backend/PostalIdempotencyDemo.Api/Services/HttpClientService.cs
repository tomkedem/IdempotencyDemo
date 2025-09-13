using PostalIdempotencyDemo.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http; // Added the missing using statement

namespace PostalIdempotencyDemo.Api.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IChaosService _chaosService;
        private readonly ILogger<HttpClientService> _logger;

        public HttpClientService(HttpClient httpClient, IChaosService chaosService, ILogger<HttpClientService> logger)
        {
            _httpClient = httpClient;
            _chaosService = chaosService;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            await ConfigureTimeoutAsync();
            _logger.LogDebug("Making GET request to {RequestUri} with timeout {Timeout}s",
                requestUri, _httpClient.Timeout.TotalSeconds);

            return await _httpClient.GetAsync(requestUri);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            await ConfigureTimeoutAsync();
            _logger.LogDebug("Making POST request to {RequestUri} with timeout {Timeout}s",
                requestUri, _httpClient.Timeout.TotalSeconds);

            return await _httpClient.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            await ConfigureTimeoutAsync();
            _logger.LogDebug("Making PUT request to {RequestUri} with timeout {Timeout}s",
                requestUri, _httpClient.Timeout.TotalSeconds);

            return await _httpClient.PutAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            await ConfigureTimeoutAsync();
            _logger.LogDebug("Making DELETE request to {RequestUri} with timeout {Timeout}s",
                requestUri, _httpClient.Timeout.TotalSeconds);

            return await _httpClient.DeleteAsync(requestUri);
        }

        public async Task<string> GetStringAsync(string requestUri)
        {
            await ConfigureTimeoutAsync();
            _logger.LogDebug("Making GET string request to {RequestUri} with timeout {Timeout}s",
                requestUri, _httpClient.Timeout.TotalSeconds);

            return await _httpClient.GetStringAsync(requestUri);
        }

        public async Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content)
        {
            await ConfigureTimeoutAsync();
            _logger.LogDebug("Making PATCH request to {RequestUri} with timeout {Timeout}s",
                requestUri, _httpClient.Timeout.TotalSeconds);

            return await _httpClient.PatchAsync(requestUri, content);
        }

        private async Task ConfigureTimeoutAsync()
        {
            // Using fixed timeout since chaos settings functionality was removed
            var timeoutSeconds = 30; // Default to 30 seconds
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            await Task.CompletedTask; // Keep async signature for consistency
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
