using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;
using Hartonomous.Core.Security;

namespace Hartonomous.Core.Billing;

public interface IBillingMeter
{
    Task<BillingUsageRecord?> MeasureAsync(
        BaseEvent evt,
        string messageType,
        string handlerName,
        AccessPolicyContext policyContext,
        CancellationToken cancellationToken = default);
}
