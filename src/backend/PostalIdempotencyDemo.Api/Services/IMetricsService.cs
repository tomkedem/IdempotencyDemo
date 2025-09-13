using PostalIdempotencyDemo.Api.Models.DTO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PostalIdempotencyDemo.Api.Services;

public interface IMetricsService
{
    Task<MetricsSummaryDto> GetMetricsSummaryAsync();
    void RecordOperation(string operationType, double responseTimeMs, bool wasSuccessful, bool wasIdempotent = false);
    void RecordChaosDisabledError(string operationType); // NEW: תיעוד שגיאות כאוס
    void ResetMetrics();
    Dictionary<string, object> GetRealTimeMetrics();
}
