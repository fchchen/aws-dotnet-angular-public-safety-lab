using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Infrastructure.Configuration;

namespace PublicSafetyLab.Infrastructure.HealthChecks;

public sealed class S3HealthCheck(
    IAmazonS3 s3,
    IOptions<AwsResourceOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.EvidenceBucketName))
        {
            return HealthCheckResult.Unhealthy("AwsResources:EvidenceBucketName is not configured.");
        }

        try
        {
            await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = options.Value.EvidenceBucketName,
                MaxKeys = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach S3.", ex);
        }
    }
}
