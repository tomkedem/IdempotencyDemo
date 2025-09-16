using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostalIdempotencyDemo.Api.Middleware
{
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception occurred");

                // Only modify response if it hasn't started yet
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var errorResponse = new { Success = false, Message = "An unexpected error occurred.", Details = ex.Message };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                }
                else
                {
                    // Response has already started, we can't modify it
                    logger.LogWarning("Cannot send error response as the response has already started");
                }
            }
        }
    }
}
