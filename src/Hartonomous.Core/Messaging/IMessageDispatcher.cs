using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Messaging;

/// <summary>
/// Dispatches broker-delivered messages to the appropriate message handlers.
/// Implementations are responsible for deserializing payloads and selecting handler instances.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Dispatches a brokered message.
    /// </summary>
    /// <param name="message">Transport message received from the broker.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    Task DispatchAsync(BrokeredMessage message, CancellationToken cancellationToken = default);
}
