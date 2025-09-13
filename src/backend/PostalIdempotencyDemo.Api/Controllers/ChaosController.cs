using Microsoft.AspNetCore.Mvc;
using PostalIdempotencyDemo.Api.Models.DTO;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChaosController : ControllerBase
{
    private readonly IChaosService _chaosService;

    public ChaosController(IChaosService chaosService)
    {
        _chaosService = chaosService;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        ChaosSettingsDto settings = await _chaosService.GetChaosSettingsAsync();
        return Ok(settings);
    }

    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] ChaosSettingsDto settingsDto)
    {
        bool success = await _chaosService.UpdateChaosSettingsAsync(settingsDto);
        if (success)
        {
            return NoContent();
        }

        return StatusCode(500, "An error occurred while updating the settings.");
    }
}
