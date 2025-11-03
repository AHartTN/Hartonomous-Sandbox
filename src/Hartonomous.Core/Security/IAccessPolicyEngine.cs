using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Security;

public interface IAccessPolicyEngine
{
    Task<AccessPolicyResult> EvaluateAsync(AccessPolicyContext context, CancellationToken cancellationToken = default);
}
