using PostalIdempotencyDemo.Api.Models;
using System.Collections.Generic;

namespace PostalIdempotencyDemo.Api.Repositories;

public interface IShipmentRepository
{
    Task<Shipment> CreateAsync(Shipment shipment);
    Task<Shipment?> GetByBarcodeAsync(string barcode);
    Task<Shipment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Shipment>> GetAllAsync();
    Task<bool> UpdateStatusAsync(Guid id, ShipmentStatus status);
    Task<bool> ExistsAsync(string barcode);
    Task<bool> UpdateAsync(Shipment shipment);
}
