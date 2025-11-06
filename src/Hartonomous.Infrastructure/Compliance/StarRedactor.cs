using Microsoft.Extensions.Compliance.Redaction;
using System;

namespace Hartonomous.Infrastructure.Compliance;

/// <summary>
/// Redacts sensitive data by replacing it with asterisks.
/// Used for Personal and Financial data classifications where redacted length provides useful context.
/// </summary>
public sealed class StarRedactor : Redactor
{
    private const string Stars = "****";
    private const int MinRedactedLength = 4;

    /// <summary>
    /// Gets the length of the redacted output (constant 4 asterisks).
    /// </summary>
    public override int GetRedactedLength(ReadOnlySpan<char> input) => Stars.Length;

    /// <summary>
    /// Redacts the source data by writing asterisks to the destination.
    /// </summary>
    /// <param name="source">The source data to redact (not used, always replaced with stars).</param>
    /// <param name="destination">The destination buffer to write the redacted data.</param>
    /// <returns>The number of characters written (always 4).</returns>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (destination.Length < Stars.Length)
        {
            throw new ArgumentException($"Destination buffer must be at least {Stars.Length} characters.", nameof(destination));
        }

        Stars.AsSpan().CopyTo(destination);
        return Stars.Length;
    }
}
