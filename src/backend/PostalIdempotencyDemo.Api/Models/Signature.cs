namespace PostalIdempotencyDemo.Api.Models
{
    public class Signature
    {
        public Guid Id { get; set; }
        public Guid ShipmentId { get; set; }
    public string? SignerName { get; set; }
    public string? SignatureDataUrl { get; set; }
    public DateTime CreatedAt { get; set; }
        public DateTime SignedAt { get; set; }
    }
}
