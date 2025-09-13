namespace PostalIdempotencyDemo.Api.Repositories
{
    /// <summary>
    /// Repository interface for data cleanup operations
    /// Single Responsibility: Handle only cleanup-related database operations
    /// </summary>
    public interface IDataCleanupRepository
    {
        /// <summary>
        /// Gets count of idempotency entries before cleanup
        /// </summary>
        Task<int> GetIdempotencyEntriesCountAsync();

        /// <summary>
        /// Gets count of operation metrics before cleanup
        /// </summary>
        Task<int> GetOperationMetricsCountAsync();

        /// <summary>
        /// Gets the oldest idempotency entry date
        /// </summary>
        Task<DateTime?> GetOldestIdempotencyEntryDateAsync();

        /// <summary>
        /// Gets the oldest metrics entry date
        /// </summary>
        Task<DateTime?> GetOldestMetricsEntryDateAsync();

        /// <summary>
        /// Deletes all idempotency entries
        /// </summary>
        Task<bool> DeleteAllIdempotencyEntriesAsync();

        /// <summary>
        /// Deletes all operation metrics
        /// </summary>
        Task<bool> DeleteAllOperationMetricsAsync();

        /// <summary>
        /// Performs complete cleanup in a transaction
        /// </summary>
        Task<bool> PerformCompleteCleanupAsync();
    }
}
