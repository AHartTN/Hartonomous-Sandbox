using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Messaging;

/// <summary>
/// Handles a single message instance using application-specific logic.
/// Implementations should be stateless or scoped per message.
/// </summary>
/// <typeparam name="TMessage">Logical message payload type.</typeparam>
public interface IMessageHandler<in TMessage>
{
    /// <summary>
    /// Processes the supplied message.
    /// </summary>
    /// <param name="message">The deserialized payload.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
