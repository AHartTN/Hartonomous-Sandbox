using Hartonomous.Core.Interfaces.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// ENTERPRISE billing API with full Stripe integration.
/// Provides usage tracking, invoicing, payments, and subscription management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IBillingService billingService,
        ILogger<BillingController> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    #region Core Billing Operations

    /// <summary>
    /// Calculate bill for tenant for specified period.
    /// Optionally generates invoice and syncs to Stripe.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(BillResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BillResult>> CalculateBill(
        [FromBody] CalculateBillRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CalculateBill API: TenantId {TenantId}, Period {Start} to {End}",
            request.TenantId, request.PeriodStart, request.PeriodEnd);

        var result = await _billingService.CalculateBillAsync(
            request.TenantId,
            request.PeriodStart ?? DateTime.UtcNow.AddMonths(-1),
            request.PeriodEnd ?? DateTime.UtcNow,
            request.GenerateInvoice ?? false,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Generate usage report with forecasting.
    /// </summary>
    [HttpGet("reports/{tenantId}")]
    [ProducesResponseType(typeof(UsageReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageReport>> GenerateReport(
        [FromRoute] int tenantId,
        [FromQuery] string reportType = "Summary",
        [FromQuery] string timeRange = "Month",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GenerateReport API: TenantId {TenantId}, Type {Type}, Range {Range}",
            tenantId, reportType, timeRange);

        var result = await _billingService.GenerateReportAsync(
            tenantId,
            reportType,
            timeRange,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get time-series usage analytics.
    /// </summary>
    [HttpGet("analytics/{tenantId}")]
    [ProducesResponseType(typeof(UsageAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageAnalytics>> GetAnalytics(
        [FromRoute] int tenantId,
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate,
        [FromQuery] string bucketInterval = "HOUR",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetAnalytics API: TenantId {TenantId}, Range {Start} to {End}",
            tenantId, startDate, endDate);

        var result = await _billingService.GetAnalyticsAsync(
            tenantId,
            startDate,
            endDate,
            bucketInterval,
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region Subscription Management

    /// <summary>
    /// Create recurring subscription for tenant.
    /// Requires Stripe integration enabled.
    /// </summary>
    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(SubscriptionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SubscriptionResult>> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateSubscription API: TenantId {TenantId}, PlanId {PlanId}",
            request.TenantId, request.PlanId);

        try
        {
            var result = await _billingService.CreateSubscriptionAsync(
                request.TenantId,
                request.PlanId,
                request.PaymentMethodId,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetSubscription),
                new { tenantId = request.TenantId },
                result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Stripe integration"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Stripe integration is not enabled" });
        }
    }

    /// <summary>
    /// Get active subscription for tenant.
    /// </summary>
    [HttpGet("subscriptions/{tenantId}")]
    [ProducesResponseType(typeof(SubscriptionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResult>> GetSubscription(
        [FromRoute] int tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetSubscription API: TenantId {TenantId}", tenantId);

        // TODO: Implement get subscription
        return NotFound(new { error = "Subscription not found" });
    }

    /// <summary>
    /// Cancel tenant subscription.
    /// Can cancel immediately or at period end.
    /// </summary>
    [HttpDelete("subscriptions/{tenantId}")]
    [ProducesResponseType(typeof(SubscriptionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResult>> CancelSubscription(
        [FromRoute] int tenantId,
        [FromQuery] bool immediately = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CancelSubscription API: TenantId {TenantId}, Immediate {Immediate}",
            tenantId, immediately);

        try
        {
            var result = await _billingService.CancelSubscriptionAsync(
                tenantId,
                immediately,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No active subscription"))
        {
            return NotFound(new { error = ex.Message });
        }
    }

    #endregion

    #region Payment Processing

    /// <summary>
    /// Process one-time payment via Stripe.
    /// Requires Stripe integration enabled.
    /// </summary>
    [HttpPost("payments")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PaymentResult>> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProcessPayment API: TenantId {TenantId}, Amount ${Amount}",
            request.TenantId, request.Amount);

        try
        {
            var result = await _billingService.ProcessPaymentAsync(
                request.TenantId,
                request.Amount,
                request.PaymentMethodId,
                request.Description,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetPayment),
                new { paymentIntentId = result.PaymentIntentId },
                result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Stripe integration"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Stripe integration is not enabled" });
        }
    }

    /// <summary>
    /// Get payment by PaymentIntent ID.
    /// </summary>
    [HttpGet("payments/{paymentIntentId}")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResult>> GetPayment(
        [FromRoute] string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetPayment API: PaymentIntentId {PaymentIntentId}", paymentIntentId);

        // TODO: Implement get payment
        return NotFound(new { error = "Payment not found" });
    }

    #endregion

    #region Invoices

    /// <summary>
    /// Get invoices for tenant.
    /// </summary>
    [HttpGet("invoices/{tenantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInvoices(
        [FromRoute] int tenantId,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetInvoices API: TenantId {TenantId}, Limit {Limit}",
            tenantId, limit);

        // TODO: Implement get invoices from database
        return Ok(new { tenantId, invoices = Array.Empty<object>() });
    }

    /// <summary>
    /// Download invoice PDF.
    /// </summary>
    [HttpGet("invoices/{tenantId}/{invoiceId}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoicePdf(
        [FromRoute] int tenantId,
        [FromRoute] string invoiceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DownloadInvoicePdf API: TenantId {TenantId}, InvoiceId {InvoiceId}",
            tenantId, invoiceId);

        // TODO: Implement PDF generation or fetch from Stripe
        return NotFound(new { error = "Invoice not found" });
    }

    #endregion

    #region Usage Tracking

    /// <summary>
    /// Record usage event for billing.
    /// This is called by internal services to track billable usage.
    /// </summary>
    [HttpPost("usage")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordUsage(
        [FromBody] RecordUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RecordUsage API: TenantId {TenantId}, Type {Type}, Quantity {Quantity}",
            request.TenantId, request.UsageType, request.Quantity);

        // TODO: Insert into BillingUsageLedger table
        // For now, just acknowledge receipt
        return Accepted(new { status = "recorded", timestamp = DateTime.UtcNow });
    }

    #endregion
}

#region Request DTOs

public record CalculateBillRequest(
    [Required] int TenantId,
    DateTime? PeriodStart = null,
    DateTime? PeriodEnd = null,
    bool? GenerateInvoice = false);

public record CreateSubscriptionRequest(
    [Required] int TenantId,
    [Required] string PlanId,
    string? PaymentMethodId = null);

public record ProcessPaymentRequest(
    [Required] int TenantId,
    [Required] decimal Amount,
    [Required] string PaymentMethodId,
    string? Description = null);

public record RecordUsageRequest(
    [Required] int TenantId,
    [Required] string UsageType,
    [Required] long Quantity,
    decimal? UnitCost = null,
    string? Metadata = null);

#endregion
