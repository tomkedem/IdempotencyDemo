using Microsoft.AspNetCore.Mvc;
using PostalIdempotencyDemo.Api.Services;

namespace PostalIdempotencyDemo.Api.Controllers
{
    /// <summary>
    /// Controller for data cleanup operations
    /// Follows Single Responsibility and provides secure cleanup operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataCleanupController(IDataCleanupService dataCleanupService, ILogger<DataCleanupController> logger) : ControllerBase
    {  
        /// <summary>
        /// Generate a confirmation token for cleanup operations
        /// </summary>
        [HttpPost("generate-token")]
        public IActionResult GenerateConfirmationToken()
        {
            string token = dataCleanupService.GenerateConfirmationToken();

            logger.LogWarning("Cleanup confirmation token requested from IP: {ClientIP}",
                HttpContext.Connection.RemoteIpAddress);

            return Ok(new
            {
                confirmationToken = token,
                expiresInMinutes = 5,
                message = "Token generated successfully. Use within 5 minutes.",
                warning = "This token will allow complete database cleanup including all historical data!"
            });
        }

        /// <summary>
        /// Get preview of what will be deleted in cleanup operation
        /// </summary>
        [HttpGet("preview")]
        public async Task<IActionResult> GetCleanupPreview()
        {
            var result = await dataCleanupService.GetCleanupPreviewAsync();

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    preview = result.Data,
                    warning = "This data will be permanently deleted and cannot be recovered!",
                    recommendation = "Consider backing up the database before proceeding."
                });
            }
            else
            {
                return BadRequest(new { error = result.ErrorMessage });
            }
        }

        /// <summary>
        /// Perform complete database cleanup with confirmation token
        /// </summary>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteCompleteCleanup([FromBody] CleanupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.ConfirmationToken))
            {
                return BadRequest(new { error = "Confirmation token is required" });
            }

            logger.LogCritical("Complete cleanup requested from IP: {ClientIP} with token: {TokenPrefix}...",
                HttpContext.Connection.RemoteIpAddress,
                request.ConfirmationToken[..Math.Min(8, request.ConfirmationToken.Length)]);

            var result = await dataCleanupService.PerformCompleteCleanupAsync(request.ConfirmationToken);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    message = result.Data,
                    timestamp = DateTime.Now,
                    warning = "All historical data has been permanently deleted!"
                });
            }
            else
            {
                return BadRequest(new { error = result.ErrorMessage });
            }
        }
    }

    /// <summary>
    /// Request model for cleanup operations
    /// </summary>
    public class CleanupRequest
    {
        public string ConfirmationToken { get; set; } = string.Empty;
    }
}
