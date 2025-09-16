using Microsoft.AspNetCore.Mvc;
using PostalIdempotencyDemo.Api.Models.DTO;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChaosController(IChaosService chaosService) : ControllerBase
{      
    /// <summary>
    /// Get current chaos settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        ChaosSettingsDto settings = await chaosService.GetChaosSettingsAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Update chaos settings with new configuration
    /// </summary>
    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] ChaosSettingsDto settingsDto)
    {
        bool success = await chaosService.UpdateChaosSettingsAsync(settingsDto);
        if (success)
        {
            return NoContent();
        }

        return StatusCode(500, "An error occurred while updating the settings.");
    }
}
