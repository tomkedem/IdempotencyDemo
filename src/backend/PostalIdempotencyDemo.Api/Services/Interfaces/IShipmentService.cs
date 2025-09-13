using PostalIdempotencyDemo.Api.DTOs;
using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Services.Interfaces;

public interface IShipmentService
{
    Task<ServiceResult<Shipment>> CreateShipmentAsync(CreateShipmentRequest request, string correlationId);
    Task<ServiceResult<IEnumerable<Shipment>>> GetAllShipmentsAsync();
    Task<ServiceResult<Shipment>> GetShipmentByBarcodeAsync(string barcode);
    Task<ServiceResult<Shipment>> UpdateShipmentStatusAsync(string barcode, ShipmentStatus status);
    Task<ServiceResult<Shipment>> UpdateShipmentStatusAsync(string barcode, ShipmentStatus status, string? notes);
}
