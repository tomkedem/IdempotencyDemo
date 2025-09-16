using Microsoft.AspNetCore.Mvc;
using PostalIdempotencyDemo.Api.Services;
using System.Threading.Tasks;

namespace PostalIdempotencyDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController(IMetricsService metricsService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await metricsService.GetMetricsSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("realtime")]
    public IActionResult GetRealTimeMetrics()
    {
        var metrics = metricsService.GetRealTimeMetrics();
        return Ok(metrics);
    }

    [HttpPost("reset")]
    public IActionResult ResetMetrics()
    {
        metricsService.ResetMetrics();
        return Ok(new { message = "Metrics reset successfully", timestamp = DateTime.Now });
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var summary = await metricsService.GetMetricsSummaryAsync();
        var realTimeMetrics = metricsService.GetRealTimeMetrics();

        return Ok(new
        {
            status = realTimeMetrics["healthStatus"],
            uptime = realTimeMetrics["uptime"],           
            operationsPerSecond = realTimeMetrics["operationsPerSecond"],
            systemLoad = realTimeMetrics["systemLoad"],
            responseTime = new
            {
                current = realTimeMetrics["currentResponseTime"],
                average = summary.AverageExecutionTimeMs               
            },
            operations = new
            {
                total = summary.TotalOperations,
                successful = summary.SuccessfulOperations,
                // errors = summary.ErrorCount,
                idempotentBlocks = summary.IdempotentHits,
                successRate = summary.SuccessRate
            },
            timestamp = DateTime.Now
        });
    }
}
