using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Security;

public interface IThrottleEvaluator
{
    Task<ThrottleResult> EvaluateAsync(ThrottleContext context, CancellationToken cancellationToken = default);
}
