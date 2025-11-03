using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Messaging;

/// <summary>
/// Represents a long-running message processing loop. The implementation is responsible for
/// receiving messages from the underlying transport and dispatching them via registered handlers.
/// </summary>
public interface IMessagePump
{
    /// <summary>
    /// Executes the pump until cancellation is requested or a terminal fault occurs.
    /// </summary>
    /// <param name="cancellationToken">Token used to request shutdown.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
