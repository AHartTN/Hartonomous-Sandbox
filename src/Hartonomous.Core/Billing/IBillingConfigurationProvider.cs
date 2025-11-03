using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Billing;

public interface IBillingConfigurationProvider
{
    Task<BillingConfiguration> GetConfigurationAsync(string tenantId, CancellationToken cancellationToken = default);
}
