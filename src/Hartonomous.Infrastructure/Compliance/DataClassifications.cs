using Microsoft.Extensions.Compliance.Classification;

namespace Hartonomous.Infrastructure.Compliance;

/// <summary>
/// Defines data classification taxonomy for Hartonomous application.
/// Classifications are used to determine which redactor to apply when logging or outputting sensitive data.
/// </summary>
public static class HartonomousDataClassifications
{
    /// <summary>
    /// Public data that can be freely logged and shared.
    /// Examples: API versions, public configuration settings, non-sensitive metadata.
    /// </summary>
    public static DataClassification Public { get; } = new DataClassification("HartonomousTaxonomy", "PublicData");

    /// <summary>
    /// Private data that should be redacted in logs but may be needed for debugging.
    /// Examples: Internal IDs, non-PII user data, system metrics.
    /// Redactor: HmacRedactor (allows correlation while protecting actual values).
    /// </summary>
    public static DataClassification Private { get; } = new DataClassification("HartonomousTaxonomy", "PrivateData");

    /// <summary>
    /// Personal data subject to privacy regulations (GDPR, CCPA).
    /// Examples: Email addresses, names, user IDs, IP addresses, tenant identifiers.
    /// Redactor: StarRedactor (masks with asterisks).
    /// </summary>
    public static DataClassification Personal { get; } = new DataClassification("HartonomousTaxonomy", "PersonalData");

    /// <summary>
    /// Sensitive data that must be completely removed from logs.
    /// Examples: Passwords, API keys, tokens, credit card numbers, SSNs.
    /// Redactor: ErasingRedactor (completely removes the value).
    /// </summary>
    public static DataClassification Sensitive { get; } = new DataClassification("HartonomousTaxonomy", "SensitiveData");

    /// <summary>
    /// Financial data subject to PCI-DSS and similar regulations.
    /// Examples: Transaction amounts, payment methods, financial identifiers.
    /// Redactor: StarRedactor (masks with asterisks).
    /// </summary>
    public static DataClassification Financial { get; } = new DataClassification("HartonomousTaxonomy", "FinancialData");

    /// <summary>
    /// Health-related data subject to HIPAA and similar regulations.
    /// Examples: Medical records, health metrics, diagnoses.
    /// Redactor: ErasingRedactor (completely removes the value).
    /// </summary>
    public static DataClassification Health { get; } = new DataClassification("HartonomousTaxonomy", "HealthData");
}
