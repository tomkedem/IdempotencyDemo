namespace PostalIdempotencyDemo.Api.Models
{
    public class Delivery
    {
        public Guid Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public DateTime DeliveryDate { get; set; }
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
        public string? RecipientName { get; set; }
        public int? StatusId { get; set; }
        public string? StatusNameHe { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
