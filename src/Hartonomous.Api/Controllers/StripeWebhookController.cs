using Hartonomous.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Stripe webhook handler for payment and subscription events.
/// Processes real-time notifications from Stripe.
/// </summary>
[ApiController]
[Route("api/webhooks/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly StripeOptions _stripeOptions;

    public StripeWebhookController(
        ILogger<StripeWebhookController> logger,
        IOptions<StripeOptions> stripeOptions)
    {
        _logger = logger;
        _stripeOptions = stripeOptions.Value;
    }

    /// <summary>
    /// Handle Stripe webhook events.
    /// Endpoint: POST /api/webhooks/stripe
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            // Verify webhook signature
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeOptions.WebhookSecret,
                throwOnApiVersionMismatch: false);

            _logger.LogInformation("Stripe webhook received: {EventType}, EventId {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            // Handle different event types
            switch (stripeEvent.Type)
            {
                // Invoice events
                case Events.InvoiceCreated:
                    await HandleInvoiceCreatedAsync(stripeEvent);
                    break;

                case Events.InvoicePaid:
                    await HandleInvoicePaidAsync(stripeEvent);
                    break;

                case Events.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailedAsync(stripeEvent);
                    break;

                case Events.InvoiceFinalized:
                    await HandleInvoiceFinalizedAsync(stripeEvent);
                    break;

                // Payment events
                case Events.PaymentIntentSucceeded:
                    await HandlePaymentSucceededAsync(stripeEvent);
                    break;

                case Events.PaymentIntentPaymentFailed:
                    await HandlePaymentFailedAsync(stripeEvent);
                    break;

                // Subscription events
                case Events.CustomerSubscriptionCreated:
                    await HandleSubscriptionCreatedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionTrialWillEnd:
                    await HandleSubscriptionTrialEndingAsync(stripeEvent);
                    break;

                // Customer events
                case Events.CustomerCreated:
                    await HandleCustomerCreatedAsync(stripeEvent);
                    break;

                case Events.CustomerUpdated:
                    await HandleCustomerUpdatedAsync(stripeEvent);
                    break;

                case Events.CustomerDeleted:
                    await HandleCustomerDeletedAsync(stripeEvent);
                    break;

                // Charge events (for refunds)
                case Events.ChargeRefunded:
                    await HandleChargeRefundedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogWarning("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error: {Message}", ex.Message);
            return BadRequest($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error: {Message}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Invoice Event Handlers

    private Task HandleInvoiceCreatedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        _logger.LogInformation("Invoice created: {InvoiceId}, Customer {CustomerId}, Amount ${Amount}",
            invoice?.Id, invoice?.CustomerId, invoice?.AmountDue / 100.0);

        // TODO: Update database with invoice creation
        return Task.CompletedTask;
    }

    private Task HandleInvoicePaidAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        _logger.LogInformation("Invoice paid: {InvoiceId}, Customer {CustomerId}, Amount ${Amount}",
            invoice?.Id, invoice?.CustomerId, invoice?.AmountPaid / 100.0);

        // TODO: Mark invoice as paid in database
        // TODO: Send payment receipt email
        // TODO: Update tenant credits/usage allowance
        return Task.CompletedTask;
    }

    private Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        _logger.LogWarning("Invoice payment failed: {InvoiceId}, Customer {CustomerId}, Attempt {Attempt}",
            invoice?.Id, invoice?.CustomerId, invoice?.AttemptCount);

        // TODO: Update invoice status in database
        // TODO: Send payment failure notification
        // TODO: Potentially suspend tenant access if multiple failures
        return Task.CompletedTask;
    }

    private Task HandleInvoiceFinalizedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        _logger.LogInformation("Invoice finalized: {InvoiceId}, Customer {CustomerId}",
            invoice?.Id, invoice?.CustomerId);

        // TODO: Update invoice status
        // TODO: Send invoice to customer
        return Task.CompletedTask;
    }

    #endregion

    #region Payment Event Handlers

    private Task HandlePaymentSucceededAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        _logger.LogInformation("Payment succeeded: {PaymentIntentId}, Amount ${Amount}",
            paymentIntent?.Id, paymentIntent?.Amount / 100.0);

        // TODO: Update payment status in database
        // TODO: Apply credits to tenant account
        return Task.CompletedTask;
    }

    private Task HandlePaymentFailedAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        _logger.LogWarning("Payment failed: {PaymentIntentId}, Reason {Reason}",
            paymentIntent?.Id, paymentIntent?.LastPaymentError?.Message);

        // TODO: Update payment status
        // TODO: Notify tenant of failure
        return Task.CompletedTask;
    }

    #endregion

    #region Subscription Event Handlers

    private Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        _logger.LogInformation("Subscription created: {SubscriptionId}, Customer {CustomerId}, Status {Status}",
            subscription?.Id, subscription?.CustomerId, subscription?.Status);

        // TODO: Store subscription in database
        // TODO: Activate tenant features based on plan
        return Task.CompletedTask;
    }

    private Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        _logger.LogInformation("Subscription updated: {SubscriptionId}, Status {Status}",
            subscription?.Id, subscription?.Status);

        // TODO: Update subscription status
        // TODO: Adjust tenant features if plan changed
        return Task.CompletedTask;
    }

    private Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        _logger.LogInformation("Subscription deleted: {SubscriptionId}, Customer {CustomerId}",
            subscription?.Id, subscription?.CustomerId);

        // TODO: Mark subscription as cancelled
        // TODO: Downgrade tenant to free tier or suspend access
        return Task.CompletedTask;
    }

    private Task HandleSubscriptionTrialEndingAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        _logger.LogInformation("Subscription trial ending: {SubscriptionId}, Ends {TrialEnd}",
            subscription?.Id, subscription?.TrialEnd);

        // TODO: Send trial ending notification
        // TODO: Prompt for payment method if not on file
        return Task.CompletedTask;
    }

    #endregion

    #region Customer Event Handlers

    private Task HandleCustomerCreatedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        _logger.LogInformation("Customer created: {CustomerId}, Email {Email}",
            customer?.Id, customer?.Email);

        // TODO: Store customer mapping in database
        return Task.CompletedTask;
    }

    private Task HandleCustomerUpdatedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        _logger.LogInformation("Customer updated: {CustomerId}", customer?.Id);

        // TODO: Sync customer details to database
        return Task.CompletedTask;
    }

    private Task HandleCustomerDeletedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        _logger.LogInformation("Customer deleted: {CustomerId}", customer?.Id);

        // TODO: Mark customer as deleted in database
        return Task.CompletedTask;
    }

    #endregion

    #region Refund Event Handlers

    private Task HandleChargeRefundedAsync(Event stripeEvent)
    {
        var charge = stripeEvent.Data.Object as Charge;
        _logger.LogInformation("Charge refunded: {ChargeId}, Amount ${Amount}",
            charge?.Id, charge?.AmountRefunded / 100.0);

        // TODO: Record refund in database
        // TODO: Adjust tenant credits
        // TODO: Send refund confirmation
        return Task.CompletedTask;
    }

    #endregion
}
