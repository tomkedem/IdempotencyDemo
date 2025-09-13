using Microsoft.AspNetCore.Mvc;
using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Controllers;

/// <summary>
/// קונטרולר לניהול פעולות אידמפוטנטיות במערכת משלוחים
/// מספק הגנה מפני פעולות כפולות באמצעות Idempotency Keys
/// </summary>
[ApiController]
[Route("api/idempotency-demo")]
public class IdempotencyDemoController : ControllerBase
{
    private readonly IIdempotencyOrchestrationService _orchestrationService;
    private readonly ILogger<IdempotencyDemoController> _logger;

    /// <summary>
    /// קונסטרקטור - מאתחל את שירות האורגניזציה
    /// </summary>
    public IdempotencyDemoController(
        IIdempotencyOrchestrationService orchestrationService,
        ILogger<IdempotencyDemoController> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// יצירת משלוח חדש עם הגנת אידמפוטנטיות
    /// </summary>
    /// <param name="request">פרטי המשלוח החדש</param>
    /// <returns>פרטי המשלוח שנוצר או התשובה השמורה אם זו בקשה כפולה</returns>
    [HttpPost("protected-delivery")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryRequest request)
    {
        // וידוא קיום מפתח אידמפוטנטיות
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues) ||
            string.IsNullOrWhiteSpace(idempotencyKeyValues.FirstOrDefault()))
        {
            return BadRequest(new { Success = false, Message = "נדרש מפתח Idempotency-Key בכותרת הבקשה" });
        }

        var idempotencyKey = idempotencyKeyValues.First()!;
        
        var requestPath = HttpContext.Request.Path.Value ?? "";

        var result = await _orchestrationService.ProcessCreateDeliveryWithIdempotencyAsync(
            request, idempotencyKey, requestPath);

        return Ok(result);
    }

    /// <summary>
    /// עדכון סטטוס משלוח עם הגנת אידמפוטנטיות
    /// </summary>
    /// <param name="barcode">ברקוד המשלוח</param>
    /// <param name="request">הסטטוס החדש</param>
    /// <returns>תוצאת העדכון או הודעה על חסימה</returns>
    [HttpPatch("protected-delivery/{barcode}/status")]
    public async Task<IActionResult> UpdateDeliveryStatus(
        [FromRoute] string barcode,
        [FromBody] UpdateDeliveryStatusRequest request)
    {
        // וידוא קיום מפתח אידמפוטנטיות
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues) ||
            string.IsNullOrWhiteSpace(idempotencyKeyValues.FirstOrDefault()))
        {
            return BadRequest(new { Success = false, Message = "נדרש מפתח Idempotency-Key בכותרת הבקשה" });
        }

        string idempotencyKey = idempotencyKeyValues.First()!;
        string requestPath = HttpContext.Request.Path.Value ?? "";

        IdempotencyDemoResponse<Shipment> result = await _orchestrationService.ProcessUpdateDeliveryStatusWithIdempotencyAsync(
            barcode, request, idempotencyKey, requestPath);

        return Ok(result);
    }

    /// <summary>
    /// דמיית תקלות רשת (לצורכי בדיקה)
    /// </summary>
    [HttpPost("simulate-network-issue")]
    public async Task<IActionResult> SimulateNetworkIssue()
    {
        _logger.LogInformation("דמיית תקלת רשת...");

        // דחיית קצרה לדמיית זמן עיבוד
        await Task.Delay(500);

        return Ok(new { Success = true, Message = "הדמיית תקלת רשת הושלמה" });
    }
}
