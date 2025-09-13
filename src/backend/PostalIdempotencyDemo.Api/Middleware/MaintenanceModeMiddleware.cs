using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MaintenanceModeMiddleware> _logger;

        public MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IChaosService chaosService)
        {
            // Skip maintenance check for health check endpoints and settings endpoints
            var path = context.Request.Path.Value?.ToLower();
            if (path != null && (path.Contains("/health") || path.Contains("/chaos/settings")))
            {
                await _next(context);
                return;
            }

            // Maintenance mode functionality removed - only keeping core idempotency settings
            // var isMaintenanceMode = await chaosService.IsMaintenanceModeAsync();
            // if (isMaintenanceMode)
            // {
            //     _logger.LogWarning("Request blocked due to maintenance mode: {Path}", context.Request.Path);
            //     
            //     context.Response.StatusCode = 503; // Service Unavailable
            //     context.Response.ContentType = "application/json";
            //     
            //     var response = new
            //     {
            //         error = "System is currently in maintenance mode",
            //         message = "The system is temporarily unavailable for maintenance. Please try again later.",
            //         statusCode = 503,
            //         timestamp = DateTime.Now
            //     };
            //
            //     await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            //     return;
            // }

            await _next(context);
        }
    }

    public static class MaintenanceModeMiddlewareExtensions
    {
        public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaintenanceModeMiddleware>();
        }
    }
}
