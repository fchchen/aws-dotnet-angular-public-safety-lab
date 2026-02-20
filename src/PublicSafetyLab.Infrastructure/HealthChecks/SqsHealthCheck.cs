using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Infrastructure.Configuration;

namespace PublicSafetyLab.Infrastructure.HealthChecks;

public sealed class SqsHealthCheck(
    IAmazonSQS sqs,
    IOptions<AwsResourceOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.IncidentQueueUrl))
        {
            return HealthCheckResult.Unhealthy("AwsResources:IncidentQueueUrl is not configured.");
        }

        try
        {
            await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = options.Value.IncidentQueueUrl,
                AttributeNames = ["QueueArn"]
            }, cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach SQS.", ex);
        }
    }
}
