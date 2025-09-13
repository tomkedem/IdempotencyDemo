using PostalIdempotencyDemo.Api.DTOs;
using PostalIdempotencyDemo.Api.Models;
using PostalIdempotencyDemo.Api.Repositories;
using PostalIdempotencyDemo.Api.Services.Interfaces;

namespace PostalIdempotencyDemo.Api.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<ShipmentService> _logger;

    public ShipmentService(IShipmentRepository repository, ILogger<ShipmentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<Shipment>> CreateShipmentAsync(CreateShipmentRequest request, string correlationId)
    {
        try
        {
            // Check if barcode already exists
            var existingShipment = await _repository.GetByBarcodeAsync(request.Barcode);

            if (existingShipment != null)
            {
                return ServiceResult<Shipment>.Failure($"Shipment with barcode {request.Barcode} already exists", "DUPLICATE_BARCODE");
            }

            var shipment = new Shipment
            {
                Barcode = request.Barcode,
                CustomerName = request.CustomerName,
                Address = request.Address,
                Weight = request.Weight,
                Price = request.Price,
                Notes = request.Notes,
                Status = ShipmentStatus.Created
            };

            var createdShipment = await _repository.CreateAsync(shipment);

            _logger.LogInformation("Created shipment {ShipmentId} with barcode {Barcode} (CorrelationId: {CorrelationId})",
                createdShipment.Id, createdShipment.Barcode, correlationId);

            return ServiceResult<Shipment>.Success(createdShipment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipment with barcode {Barcode} (CorrelationId: {CorrelationId})",
                request.Barcode, correlationId);
            return ServiceResult<Shipment>.Failure(ex.Message ?? "Unknown error occurred", "CREATE_ERROR");
        }
    }

    public async Task<ServiceResult<IEnumerable<Shipment>>> GetAllShipmentsAsync()
    {
        try
        {
            var shipments = await _repository.GetAllAsync();
            return ServiceResult<IEnumerable<Shipment>>.Success(shipments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all shipments");
            return ServiceResult<IEnumerable<Shipment>>.Failure(ex.Message ?? "Unknown error occurred", "RETRIEVAL_ERROR");
        }
    }

    public async Task<ServiceResult<Shipment>> GetShipmentByBarcodeAsync(string barcode)
    {
        try
        {
            var shipment = await _repository.GetByBarcodeAsync(barcode);

            if (shipment == null)
            {
                return ServiceResult<Shipment>.Failure($"Shipment with barcode {barcode} not found", "NOT_FOUND");
            }

            return ServiceResult<Shipment>.Success(shipment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment with barcode {Barcode}", barcode);
            return ServiceResult<Shipment>.Failure(ex.Message ?? "Unknown error occurred", "RETRIEVAL_ERROR");
        }
    }

    public async Task<ServiceResult<Shipment>> UpdateShipmentStatusAsync(string barcode, ShipmentStatus status)
    {
        return await UpdateShipmentStatusAsync(barcode, status, null);
    }

    public async Task<ServiceResult<Shipment>> UpdateShipmentStatusAsync(string barcode, ShipmentStatus status, string? notes)
    {
        try
        {
            var shipmentResult = await GetShipmentByBarcodeAsync(barcode);
            if (!shipmentResult.IsSuccess)
            {
                return ServiceResult<Shipment>.Failure(shipmentResult.ErrorMessage ?? "Unknown error occurred", shipmentResult.ErrorCode);
            }

            var shipment = shipmentResult.Data!;

            if (shipment.Status == ShipmentStatus.Cancelled)
            {
                return ServiceResult<Shipment>.Failure("Cannot update status of cancelled shipment", "INVALID_STATUS");
            }

            // Validate status transitions
            if (!shipment.Status.HasValue || !IsValidStatusTransition(shipment.Status.Value, status))
            {
                return ServiceResult<Shipment>.Failure($"Invalid status transition from {shipment.Status} to {status}", "INVALID_TRANSITION");
            }

            shipment.Status = status;
            shipment.UpdatedAt = DateTime.Now;
            if (notes != null)
            {
                shipment.Notes = notes;
            }

            await _repository.UpdateAsync(shipment);

            _logger.LogInformation("Updated shipment {ShipmentId} status from {OldStatus} to {NewStatus}",
                shipment.Id, shipment.Status, status);

            return ServiceResult<Shipment>.Success(shipment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipment status for barcode {Barcode}", barcode);
            return ServiceResult<Shipment>.Failure(ex.Message ?? "Unknown error occurred", "UPDATE_ERROR");
        }
    }

    public async Task<ServiceResult> CancelShipmentAsync(string barcode)
    {
        try
        {
            var shipmentResult = await GetShipmentByBarcodeAsync(barcode);
            if (!shipmentResult.IsSuccess)
            {
                return ServiceResult.Failure(shipmentResult.ErrorMessage ?? "Unknown error occurred", shipmentResult.ErrorCode);
            }

            var shipment = shipmentResult.Data!;

            if (shipment.Status == ShipmentStatus.Delivered)
            {
                return ServiceResult.Failure("Cannot cancel delivered shipment", "INVALID_STATUS");
            }

            if (shipment.Status == ShipmentStatus.Cancelled)
            {
                return ServiceResult.Failure("Shipment is already cancelled", "INVALID_STATUS");
            }

            shipment.Status = ShipmentStatus.Cancelled;
            shipment.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(shipment);

            _logger.LogInformation("Cancelled shipment {ShipmentId} with barcode {Barcode}",
                shipment.Id, shipment.Barcode);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling shipment with barcode {Barcode}", barcode);
            return ServiceResult.Failure(ex.Message ?? "Unknown error occurred", "UPDATE_ERROR");
        }
    }

    private static bool IsValidStatusTransition(ShipmentStatus currentStatus, ShipmentStatus newStatus)
    {
        return currentStatus switch
        {
            ShipmentStatus.Created => newStatus is ShipmentStatus.InTransit or ShipmentStatus.Cancelled,
            ShipmentStatus.InTransit => newStatus is ShipmentStatus.Delivered or ShipmentStatus.Failed,
            ShipmentStatus.Delivered => false, // Cannot change from delivered
            ShipmentStatus.Cancelled => false, // Cannot change from cancelled
            ShipmentStatus.Failed => newStatus is ShipmentStatus.InTransit, // Can retry failed shipments
            _ => false
        };
    }
}
