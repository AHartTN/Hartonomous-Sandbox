using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Billing;

/// <summary>
/// Service for billing and usage operations.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Calculates billing based on usage ledger for period.
    /// Calls sp_CalculateBill stored procedure.
    /// </summary>
    Task<BillResult> CalculateBillAsync(
        int tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates usage reports (Summary/Detailed/Forecast).
    /// Calls sp_GenerateUsageReport stored procedure.
    /// </summary>
    Task<UsageReport> GenerateReportAsync(
        int tenantId,
        string reportType = "Summary",
        string timeRange = "Month",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves usage analytics with aggregations.
    /// Calls sp_GetUsageAnalytics stored procedure.
    /// </summary>
    Task<UsageAnalytics> GetAnalyticsAsync(
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

public record BillResult(
    int TenantId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TotalAmount,
    IEnumerable<BillLineItem> LineItems);

public record BillLineItem(
    string UsageType,
    long Quantity,
    decimal UnitPrice,
    decimal Amount);

public record UsageReport(
    int TenantId,
    string ReportType,
    DateTime GeneratedAt,
    string ReportDataJson);

public record UsageAnalytics(
    int TenantId,
    long TotalInferences,
    long TotalAtoms,
    long TotalTokens,
    decimal AverageLatencyMs,
    IEnumerable<DailyUsage> DailyBreakdown);

public record DailyUsage(
    DateTime Date,
    long Inferences,
    long Tokens,
    decimal Cost);
