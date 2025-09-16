using System.Diagnostics;

namespace PostalIdempotencyDemo.Api.Middleware
{
    public class ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
    {
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

                logger.LogDebug("Request {Method} {Path} completed in {ResponseTime}ms",
                    context.Request.Method,
                    context.Request.Path,
                    responseTime);

                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
