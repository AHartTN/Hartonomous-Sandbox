using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Billing;

/// <summary>
/// Service for billing and usage operations with Stripe integration.
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
        bool generateInvoice = false,
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
        string bucketInterval = "HOUR",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a subscription for a tenant using Stripe.
    /// </summary>
    Task<SubscriptionResult> CreateSubscriptionAsync(
        int tenantId,
        string planId,
        string? paymentMethodId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a subscription for a tenant.
    /// </summary>
    Task<SubscriptionResult> CancelSubscriptionAsync(
        int tenantId,
        bool immediately = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a one-time payment using Stripe.
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(
        int tenantId,
        decimal amount,
        string paymentMethodId,
        string? description = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Bill calculation result with line items.
/// </summary>
public record BillResult(
    int TenantId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    IEnumerable<UsageLineItem> UsageBreakdown);

/// <summary>
/// Individual usage line item in a bill.
/// </summary>
public record UsageLineItem(
    string UsageType,
    long TotalQuantity,
    decimal TotalCost);

/// <summary>
/// Usage report result.
/// </summary>
public record UsageReport(
    int TenantId,
    string ReportType,
    string TimeRange,
    string ReportJson,
    DateTime GeneratedAt);

/// <summary>
/// Usage analytics with time-series data.
/// </summary>
public record UsageAnalytics(
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    string BucketInterval,
    IEnumerable<TimeBucket> TimeSeries);

/// <summary>
/// Time bucket for usage analytics.
/// </summary>
public record TimeBucket(
    DateTime BucketStart,
    long EventCount,
    decimal Cost);

/// <summary>
/// Stripe subscription result.
/// </summary>
public record SubscriptionResult(
    string SubscriptionId,
    int TenantId,
    string PlanId,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd);

/// <summary>
/// Stripe payment result.
/// </summary>
public record PaymentResult(
    string PaymentIntentId,
    int TenantId,
    decimal Amount,
    string Status,
    DateTime ProcessedAt);
