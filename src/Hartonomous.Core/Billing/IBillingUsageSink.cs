using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Billing;

public interface IBillingUsageSink
{
    Task WriteAsync(BillingUsageRecord record, CancellationToken cancellationToken = default);
}
