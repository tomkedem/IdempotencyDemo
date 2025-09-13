using Microsoft.Data.SqlClient;
using PostalIdempotencyDemo.Api.Models.DTO;

namespace PostalIdempotencyDemo.Api.Repositories
{
    public interface IMetricsRepository
    {
        Task LogMetricsAsync(string operationType, string endpoint, long executionTimeMs, bool isIdempotentHit, string? idempotencyKey, bool isError = false);
        Task<MetricsSummaryDto> GetMetricsSummaryAsync();
    }

    public class MetricsRepository : IMetricsRepository
    {
        private readonly Data.ISqlExecutor _sqlExecutor;
        private readonly ILogger<MetricsRepository> _logger;

        public MetricsRepository(Data.ISqlExecutor sqlExecutor, ILogger<MetricsRepository> logger)
        {
            _sqlExecutor = sqlExecutor;
            _logger = logger;
        }

        public async Task LogMetricsAsync(string operationType, string endpoint, long executionTimeMs, bool isIdempotentHit, string? idempotencyKey, bool isError = false)
        {
            try
            {
                await _sqlExecutor.ExecuteAsync(async connection =>
                {
                    const string sql = @"
                        INSERT INTO operation_metrics (id, operation_type, endpoint, execution_time_ms, 
                                                     is_idempotent_hit, idempotency_key, is_error, created_at)
                        VALUES (@id, @operation_type, @endpoint, @execution_time_ms, 
                                @is_idempotent_hit, @idempotency_key, @is_error, @created_at)";

                    using var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@id", Guid.NewGuid());
                    command.Parameters.AddWithValue("@operation_type", operationType);
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    command.Parameters.AddWithValue("@execution_time_ms", executionTimeMs);
                    command.Parameters.AddWithValue("@is_idempotent_hit", isIdempotentHit);
                    command.Parameters.AddWithValue("@idempotency_key", idempotencyKey ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@is_error", isError);
                    command.Parameters.AddWithValue("@created_at", DateTime.Now);

                    await command.ExecuteNonQueryAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging metrics");
            }
        }

        public async Task<MetricsSummaryDto> GetMetricsSummaryAsync()
        {
            var summary = new MetricsSummaryDto();

            await _sqlExecutor.ExecuteAsync(async connection =>
            {
                const string sql = @"
                    SELECT 
                        COUNT(*) AS TotalOperations,                            
                        SUM(CASE WHEN operation_type LIKE '%idempotent_block' THEN 1 ELSE 0 END) AS IdempotentHits,
                        SUM(CASE WHEN operation_type LIKE '%update_status' THEN 1 ELSE 0 END) AS SuccessfulOperations, 
                        SUM(CASE WHEN operation_type LIKE '%update_status_Idempotency_disabled' THEN 1 ELSE 0 END) AS ChaosDisabledErrors,
                        AVG(CAST(NULLIF(execution_time_ms, 0) AS FLOAT)) AS AverageExecutionTimeMs,
                        CAST( 
                            
                                (SUM(CASE WHEN operation_type LIKE '%idempotent_block' OR operation_type LIKE '%update_status' THEN 1 ELSE 0 END) * 100.0) / NULLIF(COUNT(*), 0) 
                            
                        AS FLOAT) AS SuccessRate                          
                    FROM operation_metrics;";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    summary.TotalOperations = reader["TotalOperations"] as int? ?? 0;
                    summary.IdempotentHits = reader["IdempotentHits"] as int? ?? 0;
                    summary.SuccessfulOperations = reader["SuccessfulOperations"] as int? ?? 0;
                    summary.ChaosDisabledErrors = reader["ChaosDisabledErrors"] as int? ?? 0;
                    summary.AverageExecutionTimeMs = reader["AverageExecutionTimeMs"] as double? ?? 0;
                    summary.SuccessRate = reader["SuccessRate"] as double? ?? 100.0;
                }
            });

            return summary;
        }
    }
}
