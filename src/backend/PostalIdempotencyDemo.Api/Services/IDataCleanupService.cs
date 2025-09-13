using PostalIdempotencyDemo.Api.Models;

namespace PostalIdempotencyDemo.Api.Services
{
    /// <summary>
    /// Interface for handling complete data cleanup operations with safety measures
    /// </summary>
    public interface IDataCleanupService
    {
        /// <summary>
        /// Performs complete database cleanup after validation
        /// </summary>
        /// <param name="confirmationToken">Security token to confirm the operation</param>
        /// <returns>Result of the cleanup operation</returns>
        Task<ServiceResult<string>> PerformCompleteCleanupAsync(string confirmationToken);

        /// <summary>
        /// Generates a confirmation token for secure cleanup operations
        /// </summary>
        /// <returns>Temporary confirmation token</returns>
        string GenerateConfirmationToken();

        /// <summary>
        /// Validates if a confirmation token is valid and not expired
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>True if token is valid</returns>
        bool ValidateConfirmationToken(string token);

        /// <summary>
        /// Gets statistics about what will be deleted before cleanup
        /// </summary>
        /// <returns>Cleanup preview information</returns>
        Task<ServiceResult<CleanupPreview>> GetCleanupPreviewAsync();
    }

    /// <summary>
    /// Information about what will be deleted in cleanup operation
    /// </summary>
    public class CleanupPreview
    {
        public int IdempotencyEntriesCount { get; set; }
        public int OperationMetricsCount { get; set; }
        public DateTime OldestIdempotencyEntry { get; set; }
        public DateTime OldestMetricEntry { get; set; }
        public long EstimatedDataSizeKB { get; set; }
    }
}
