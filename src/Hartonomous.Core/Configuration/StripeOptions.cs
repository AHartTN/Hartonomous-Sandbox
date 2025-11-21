using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Stripe integration configuration.
/// Supports sandbox (test) and production modes.
/// </summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Enable/disable Stripe integration.
    /// When false, billing operates in local-only mode.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Stripe operation mode: 'test' or 'live'.
    /// Test mode uses test API keys and does not process real payments.
    /// </summary>
    [Required]
    public string Mode { get; set; } = "test";

    /// <summary>
    /// Stripe secret API key.
    /// Test keys start with 'sk_test_', live keys start with 'sk_live_'.
    /// NEVER commit this to source control - use User Secrets or Azure Key Vault.
    /// </summary>
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe publishable API key.
    /// Test keys start with 'pk_test_', live keys start with 'pk_live_'.
    /// Safe to expose in client-side code.
    /// </summary>
    [Required]
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signing secret for validating Stripe webhook events.
    /// Starts with 'whsec_'.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Default currency for billing operations.
    /// </summary>
    public string DefaultCurrency { get; set; } = "usd";

    /// <summary>
    /// Automatically create invoices in Stripe when bills are generated.
    /// </summary>
    public bool AutoSyncInvoices { get; set; } = true;

    /// <summary>
    /// Default payment terms in days.
    /// </summary>
    public int DefaultPaymentTermsDays { get; set; } = 30;

    /// <summary>
    /// Enable automatic payment retries for failed subscriptions.
    /// </summary>
    public bool EnableSmartRetries { get; set; } = true;
}
