using System.ComponentModel.DataAnnotations;

namespace PostalIdempotencyDemo.Api.Models;

// Generic API Response
public class IdempotencyDemoResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public int ExecutionTimeMs { get; set; }
    public bool WasIdempotentHit { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}


// DTOs for API Requests


public class UpdateDeliveryStatusRequest
{
    [Required]
    public int StatusId { get; set; }
}

public class SignatureRequest
{
    [Required]
    public string Barcode { get; set; } = string.Empty;

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    public string? SignatureData { get; set; }

    [Range(1, 3)]
    public int SignatureType { get; set; } = 1;

    public string? SignerName { get; set; }
}

// Configuration Models

public class NetworkSimulationSettings
{
    public bool EnableNetworkIssues { get; set; } = false;
    public int MinDelayMs { get; set; } = 100;
    public int MaxDelayMs { get; set; } = 3000;
    public int FailurePercentage { get; set; } = 10;
    public int TimeoutMs { get; set; } = 5000;
}
