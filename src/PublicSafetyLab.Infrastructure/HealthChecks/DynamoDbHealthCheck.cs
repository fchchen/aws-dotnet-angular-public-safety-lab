using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Infrastructure.Configuration;

namespace PublicSafetyLab.Infrastructure.HealthChecks;

public sealed class DynamoDbHealthCheck(
    IAmazonDynamoDB dynamoDb,
    IOptions<AwsResourceOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.IncidentTableName))
        {
            return HealthCheckResult.Unhealthy("AwsResources:IncidentTableName is not configured.");
        }

        try
        {
            await dynamoDb.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = options.Value.IncidentTableName
            }, cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach DynamoDB.", ex);
        }
    }
}
