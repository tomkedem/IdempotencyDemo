using System.ComponentModel.DataAnnotations;
using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.DTOs;

public class UpdateStatusRequest
{
    [Required]
    public ShipmentStatus Status { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}
