using System.ComponentModel.DataAnnotations;

namespace PostalIdempotencyDemo.Api.Models;

public class CreateDeliveryRequest
{
    [Required]
    public string Barcode { get; set; } = string.Empty;

    public string? EmployeeId { get; set; }

    public double? LocationLat { get; set; }

    public double? LocationLng { get; set; }

    public string? RecipientName { get; set; }

    [Required]
    public int DeliveryStatus { get; set; }

    public string? Notes { get; set; }
}
