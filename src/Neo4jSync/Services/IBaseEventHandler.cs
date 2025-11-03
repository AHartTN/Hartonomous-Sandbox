using Hartonomous.Core.Messaging;
using Hartonomous.Core.Models;

namespace Hartonomous.Neo4jSync.Services;

public interface IBaseEventHandler : IMessageHandler<BaseEvent>
{
    bool CanHandle(BaseEvent message);
}
