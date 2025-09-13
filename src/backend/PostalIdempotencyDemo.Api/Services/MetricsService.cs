using PostalIdempotencyDemo.Api.Models.DTO;
using PostalIdempotencyDemo.Api.Repositories;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PostalIdempotencyDemo.Api.Services;

public class MetricsService : IMetricsService
{
    private readonly IMetricsRepository _metricsRepository;
    private readonly ILogger<MetricsService> _logger;

    // In-memory metrics for real-time tracking
    private static readonly ConcurrentQueue<double> _responseTimeHistory = new();
    private static readonly ConcurrentDictionary<string, long> _operationCounters = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastOperationTimes = new();

    // Performance tracking
    private static double _averageResponseTime = 0.0;
    private static int _totalOperations = 0;
    private static int _successfulOperations = 0;
    private static int _idempotentBlocks = 0;
    private static int _errorCount = 0;
    private static int _chaosDisabledErrors = 0; // NEW: שגיאות כאשר הגנה כבויה
    private static DateTime _lastResetTime = DateTime.Now;

    public MetricsService(IMetricsRepository metricsRepository, ILogger<MetricsService> logger)
    {
        _metricsRepository = metricsRepository;
        _logger = logger;

        // Add some sample response times for testing
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Add sample response times to test the average calculation
        var sampleTimes = new double[] { 150.5, 200.3, 175.8, 220.1, 190.7, 165.2, 180.9, 195.4, 210.6, 185.3 };

        foreach (var time in sampleTimes)
        {
            _responseTimeHistory.Enqueue(time);
        }

        // Update the average
        _averageResponseTime = CalculateCurrentAverageResponseTime();

        _logger.LogInformation("Initialized sample response times. Current average: {Average}ms", _averageResponseTime);
    }

    public async Task<MetricsSummaryDto> GetMetricsSummaryAsync()
    {
        var dbSummary = await _metricsRepository.GetMetricsSummaryAsync();

        // Use only database metrics (no double counting with in-memory metrics)
        return new MetricsSummaryDto
        {
            TotalOperations = dbSummary.TotalOperations,
            IdempotentHits = dbSummary.IdempotentHits,
            SuccessfulOperations = dbSummary.SuccessfulOperations,
            ChaosDisabledErrors = dbSummary.ChaosDisabledErrors,
            AverageExecutionTimeMs = dbSummary.AverageExecutionTimeMs, // ✅ משתמש בנתונים מהDatabase
            SuccessRate = dbSummary.SuccessRate, // ✅ משתמש בחישוב מה-SQL
            LastUpdated = DateTime.Now

        };
    }

    public void RecordOperation(string operationType, double responseTimeMs, bool wasSuccessful, bool wasIdempotent = false)
    {
        // Record the response time
        RecordResponseTime(responseTimeMs);

        // Record the operation outcome
        Interlocked.Increment(ref _totalOperations);

        if (wasSuccessful)
        {
            // כל פעולה מוצלחת נחשבת כהצלחה, גם חסימות!
            Interlocked.Increment(ref _successfulOperations);
        }
        else
        {
            Interlocked.Increment(ref _errorCount);
        }

        if (wasIdempotent)
        {
            // רישום נוסף שזו חסימה אידמפוטנטית (סוג מיוחד של הצלחה)
            Interlocked.Increment(ref _idempotentBlocks);
        }

        // Track operation type
        _operationCounters.AddOrUpdate(operationType, 1, (key, value) => value + 1);
        _lastOperationTimes[operationType] = DateTime.Now;

        _logger.LogDebug("Operation recorded - Type: {Type}, Success: {Success}, Idempotent: {Idempotent}, ResponseTime: {ResponseTime}ms, Total: {Total}",
            operationType, wasSuccessful, wasIdempotent, responseTimeMs, _totalOperations);
    }

    public void RecordChaosDisabledError(string operationType)
    {
        Interlocked.Increment(ref _chaosDisabledErrors);
        Interlocked.Increment(ref _totalOperations);

        // Track operation type
        _operationCounters.AddOrUpdate($"{operationType}_chaos_error", 1, (key, value) => value + 1);
        _lastOperationTimes[$"{operationType}_chaos_error"] = DateTime.Now;

        _logger.LogWarning("Chaos disabled error recorded - Type: {Type}, Total Chaos Errors: {ChaosErrors}",
            operationType, _chaosDisabledErrors);
    }

    public void RecordResponseTime(double responseTimeMs)
    {
        _responseTimeHistory.Enqueue(responseTimeMs);

        // Keep only last 100 measurements for rolling average
        while (_responseTimeHistory.Count > 100)
        {
            _responseTimeHistory.TryDequeue(out _);
        }

        // Update average response time
        _averageResponseTime = CalculateCurrentAverageResponseTime();

        _logger.LogDebug("Response time recorded: {ResponseTime}ms, Current average: {Average}ms",
            responseTimeMs, _averageResponseTime);
    }

    public void ResetMetrics()
    {
        _responseTimeHistory.Clear();
        _operationCounters.Clear();
        _lastOperationTimes.Clear();

        _totalOperations = 0;
        _successfulOperations = 0;
        _idempotentBlocks = 0;
        _errorCount = 0;
        _chaosDisabledErrors = 0; // NEW: איפוס שגיאות כאוס
        _averageResponseTime = 0.0;
        _lastResetTime = DateTime.Now;

        _logger.LogInformation("Metrics reset at {ResetTime}", _lastResetTime);
    }

    public Dictionary<string, object> GetRealTimeMetrics()
    {
        return new Dictionary<string, object>
        {
            ["currentResponseTime"] = GetCurrentResponseTime(),
            ["operationsPerSecond"] = CalculateOperationsPerSecond(),
            ["systemLoad"] = CalculateSystemLoad(),
            ["memoryUsage"] = GC.GetTotalMemory(false) / (1024 * 1024), // MB
            ["activeConnections"] = _operationCounters.Count,
            ["uptime"] = DateTime.Now - _lastResetTime,
            ["healthStatus"] = CalculateSystemHealth()
        };
    }

    private double CalculateCurrentAverageResponseTime()
    {
        if (_responseTimeHistory.IsEmpty)
            return 0.0;

        var times = _responseTimeHistory.ToArray();
        return times.Length > 0 ? times.Average() : 0.0;
    }

    private string CalculateSystemHealth()
    {
        // השתמש בערך קבוע כרגע, יתעדכן בעתיד מהDatabase
        var avgResponseTime = _averageResponseTime;

        // הערכה פשוטה על בסיס זמן תגובה בלבד
        if (avgResponseTime < 100)
            return "Excellent";
        else if (avgResponseTime < 500)
            return "Good";
        else if (avgResponseTime < 1000)
            return "Fair";
        else
            return "Poor";
    }

    private double CalculateThroughput()
    {
        var timeSpan = DateTime.Now - _lastResetTime;
        return timeSpan.TotalMinutes > 0 ? _totalOperations / timeSpan.TotalMinutes : 0;
    }

    private double GetPeakResponseTime()
    {
        return _responseTimeHistory.IsEmpty ? 0.0 : _responseTimeHistory.Max();
    }

    private double GetMinResponseTime()
    {
        return _responseTimeHistory.IsEmpty ? 0.0 : _responseTimeHistory.Min();
    }

    private double GetCurrentResponseTime()
    {
        return _responseTimeHistory.IsEmpty ? 0.0 : _responseTimeHistory.Last();
    }

    private double CalculateOperationsPerSecond()
    {
        var timeSpan = DateTime.Now - _lastResetTime;
        return timeSpan.TotalSeconds > 0 ? _totalOperations / timeSpan.TotalSeconds : 0;
    }

    private double CalculateSystemLoad()
    {
        // Simple load calculation based on recent activity
        var recentOperations = _operationCounters.Values.Sum();
        return Math.Min(recentOperations / 100.0, 1.0) * 100; // Normalize to percentage
    }
}
