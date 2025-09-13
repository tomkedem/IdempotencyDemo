using System.Diagnostics;

namespace PostalIdempotencyDemo.Api.Middleware
{
    public class ResponseTimeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseTimeMiddleware> _logger;

        public ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Hook into the response starting event to add the header before response begins
            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                var responseTime = stopwatch.ElapsedMilliseconds;

                if (!context.Response.HasStarted)
                {
                    context.Response.Headers["X-Response-Time"] = $"{responseTime}ms";
                }

                _logger.LogDebug("Request {Method} {Path} completed in {ResponseTime}ms",
                    context.Request.Method,
                    context.Request.Path,
                    responseTime);

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
