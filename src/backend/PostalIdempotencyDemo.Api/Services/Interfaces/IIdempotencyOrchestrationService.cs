using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Services.Interfaces
{
    /// <summary>
    /// שירות ארגון הגנה אידמפוטנטית - מתאם בין שירותי האידמפוטנטיות והעסקיים
    /// </summary>
    public interface IIdempotencyOrchestrationService
    {
        /// <summary>
        /// עיבוד יצירת משלוח עם הגנה אידמפוטנטית מלאה
        /// </summary>
        Task<IdempotencyDemoResponse<Delivery>> ProcessCreateDeliveryWithIdempotencyAsync(
            CreateDeliveryRequest request,
            string idempotencyKey,            
            string requestPath);

        /// <summary>
        /// עיבוד עדכון סטטוס משלוח עם הגנה אידמפוטנטית מלאה
        /// </summary>
        Task<IdempotencyDemoResponse<Shipment>> ProcessUpdateDeliveryStatusWithIdempotencyAsync(
            string barcode,
            UpdateDeliveryStatusRequest request,
            string idempotencyKey,
            string requestPath);
    }
}
