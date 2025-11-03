using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Security;

public interface IAccessPolicyRule
{
    Task<AccessPolicyResult?> EvaluateAsync(AccessPolicyContext context, CancellationToken cancellationToken = default);
}
