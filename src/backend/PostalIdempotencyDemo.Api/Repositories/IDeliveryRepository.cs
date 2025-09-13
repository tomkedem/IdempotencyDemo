namespace PostalIdempotencyDemo.Api.Repositories
{
    public interface IDeliveryRepository
    {
        Task CreateDeliveryAsync(object delivery);
        Task<Models.Shipment?> UpdateDeliveryStatusAsync(string barcode, int statusId);
        Task<Models.Shipment?> GetShipmentByBarcodeAsync(string barcode);
        Task<Models.Delivery?> GetDeliveryByBarcodeAsync(string barcode);
    }

    // ...existing code...
}
