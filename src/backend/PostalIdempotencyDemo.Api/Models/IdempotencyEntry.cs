using System.ComponentModel.DataAnnotations;

namespace PostalIdempotencyDemo.Api.Models;

public class IdempotencyEntry
{
    public string? RequestHash { get; set; }
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public string? ResponseData { get; set; }

    [Required]
    public int StatusCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; }

    [Required]
    [StringLength(100)]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string HttpMethod { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Operation { get; set; }

    [StringLength(100)]
    public string? CorrelationId { get; set; }

    [StringLength(50)]
    public string? RelatedEntityId { get; set; }
}
