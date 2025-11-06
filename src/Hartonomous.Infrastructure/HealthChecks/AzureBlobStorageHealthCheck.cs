using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hartonomous.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Azure Blob Storage connectivity.
/// </summary>
public class AzureBlobStorageHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageHealthCheck(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to get account info to verify connectivity
            var accountInfo = await _blobServiceClient.GetAccountInfoAsync(cancellationToken);

            if (accountInfo?.Value != null)
            {
                var data = new Dictionary<string, object>
                {
                    ["AccountKind"] = accountInfo.Value.AccountKind.ToString(),
                    ["SkuName"] = accountInfo.Value.SkuName.ToString()
                };

                return HealthCheckResult.Healthy("Azure Blob Storage connection successful", data: data);
            }

            return HealthCheckResult.Degraded("Azure Blob Storage returned unexpected response");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure Blob Storage connection failed", ex);
        }
    }
}
