namespace PostalIdempotencyDemo.Api.Models.DTO;

public class MetricsSummaryDto
{
    public int TotalOperations { get; set; }
    public int IdempotentHits { get; set; }
    public int SuccessfulOperations { get; set; }
    public int ChaosDisabledErrors { get; set; } // NEW: שגיאות כאשר הגנה כבויה
    public double AverageExecutionTimeMs { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastUpdated { get; set; } 
}
