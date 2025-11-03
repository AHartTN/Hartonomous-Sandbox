using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Configuration surface for the SQL Server Service Broker message transport.
/// Values default to the Hartonomous baseline but can be overridden per environment.
/// </summary>
public sealed class MessageBrokerOptions
{
    /// <summary>
    /// Configuration section name used to bind options.
    /// </summary>
    public const string SectionName = "MessageBroker";

    /// <summary>
    /// Fully-qualified SQL identifier of the initiator service.
    /// </summary>
    [Required]
    public string InitiatorServiceName { get; set; } = "Hartonomous.BrokerInitiator";

    /// <summary>
    /// Fully-qualified SQL identifier of the target service that owns the queue.
    /// </summary>
    [Required]
    public string TargetServiceName { get; set; } = "Hartonomous.BrokerTarget";

    /// <summary>
    /// Contract name describing allowed message types.
    /// </summary>
    [Required]
    public string ContractName { get; set; } = "//Hartonomous/Contract";

    /// <summary>
    /// Message type name assigned to outbound payloads.
    /// </summary>
    [Required]
    public string MessageTypeName { get; set; } = "//Hartonomous/Message";

    /// <summary>
    /// Fully-qualified queue name (schema + object).
    /// </summary>
    [Required]
    public string QueueName { get; set; } = "dbo.HartonomousMessageQueue";

    /// <summary>
    /// Milliseconds to wait for a message before returning null from ReceiveAsync.
    /// </summary>
    [Range(250, 60_000)]
    public int ReceiveWaitTimeoutMilliseconds { get; set; } = 2_000;

    /// <summary>
    /// Optional maximum size (in characters) for message bodies. Enforced client-side to guard against runaway payloads.
    /// </summary>
    [Range(1_024, 2_097_152)]
    public int MaxMessageCharacters { get; set; } = 512_000; // ~1MB UTF-16

    /// <summary>
    /// Conversation lifetime in seconds. Short-lived since we treat each message as one-shot.
    /// </summary>
    [Range(30, 3_600)]
    public int ConversationLifetimeSeconds { get; set; } = 300;
}
