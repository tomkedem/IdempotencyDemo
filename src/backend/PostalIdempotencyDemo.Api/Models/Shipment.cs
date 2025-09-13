using System.ComponentModel.DataAnnotations;

namespace PostalIdempotencyDemo.Api.Models;

public class Shipment
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Barcode { get; set; } = string.Empty;       
    
    [StringLength(100)]
    public string? CustomerName { get; set; }
    
    [StringLength(200)]
    public string? Address { get; set; }
    
    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal? Weight { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }
    
    public int? StatusId { get; set; }
    public ShipmentStatus? Status { get; set; }
    public string? StatusName { get; set; }
    public string? StatusNameHe { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

public enum ShipmentStatus
{
    Created = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4,
    Failed = 5
}
