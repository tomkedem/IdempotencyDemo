using Microsoft.Data.SqlClient;
using PostalIdempotencyDemo.Api.Data;

namespace PostalIdempotencyDemo.Api.Repositories
{
    /// <summary>
    /// Repository implementation for data cleanup operations
    /// Follows Single Responsibility Principle - handles only cleanup operations
    /// </summary>
    public class DataCleanupRepository : IDataCleanupRepository
    {
        private readonly ISqlExecutor _sqlExecutor;
        private readonly ILogger<DataCleanupRepository> _logger;

        public DataCleanupRepository(ISqlExecutor sqlExecutor, ILogger<DataCleanupRepository> logger)
        {
            _sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> GetIdempotencyEntriesCountAsync()
        {
            const string sql = "SELECT COUNT(*) FROM idempotency_entries";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            });
        }

        public async Task<int> GetOperationMetricsCountAsync()
        {
            const string sql = "SELECT COUNT(*) FROM operation_metrics";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            });
        }

        public async Task<DateTime?> GetOldestIdempotencyEntryDateAsync()
        {
            const string sql = "SELECT MIN(created_at) FROM idempotency_entries";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                var result = await command.ExecuteScalarAsync();
                return result == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(result);
            });
        }

        public async Task<DateTime?> GetOldestMetricsEntryDateAsync()
        {
            const string sql = "SELECT MIN(created_at) FROM operation_metrics";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                var result = await command.ExecuteScalarAsync();
                return result == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(result);
            });
        }

        public async Task<bool> DeleteAllIdempotencyEntriesAsync()
        {
            const string sql = "DELETE FROM idempotency_entries";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                var rowsAffected = await command.ExecuteNonQueryAsync();

                _logger.LogWarning("Deleted {RowsAffected} idempotency entries", rowsAffected);
                return true;
            });
        }

        public async Task<bool> DeleteAllOperationMetricsAsync()
        {
            const string sql = "DELETE FROM operation_metrics";

            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                var rowsAffected = await command.ExecuteNonQueryAsync();

                _logger.LogWarning("Deleted {RowsAffected} operation metrics entries", rowsAffected);
                return true;
            });
        }

        public async Task<bool> PerformCompleteCleanupAsync()
        {
            return await _sqlExecutor.ExecuteAsync(async connection =>
            {
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Delete operation metrics first (may have foreign key references)
                    using (var command = new SqlCommand("DELETE FROM operation_metrics", connection, transaction))
                    {
                        var metricsDeleted = await command.ExecuteNonQueryAsync();
                        _logger.LogWarning("Deleted {Count} operation metrics in transaction", metricsDeleted);
                    }

                    // Delete idempotency entries
                    using (var command = new SqlCommand("DELETE FROM idempotency_entries", connection, transaction))
                    {
                        var entriesDeleted = await command.ExecuteNonQueryAsync();
                        _logger.LogWarning("Deleted {Count} idempotency entries in transaction", entriesDeleted);
                    }

                    transaction.Commit();
                    _logger.LogWarning("Complete database cleanup completed successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Failed to perform complete cleanup, transaction rolled back");
                    throw;
                }
            });
        }
    }
}
