using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Messaging;

public interface IMessageDeadLetterSink
{
    Task WriteAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
}
