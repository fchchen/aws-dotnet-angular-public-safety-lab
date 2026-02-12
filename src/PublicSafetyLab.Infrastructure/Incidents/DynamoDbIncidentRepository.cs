using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Incidents;
using PublicSafetyLab.Infrastructure.Configuration;

namespace PublicSafetyLab.Infrastructure.Incidents;

public sealed class DynamoDbIncidentRepository(
    IAmazonDynamoDB dynamoDb,
    IOptions<AwsResourceOptions> options) : IIncidentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(Incident incident, CancellationToken cancellationToken)
    {
        var snapshot = incident.ToSnapshot();
        var payload = JsonSerializer.Serialize(snapshot, JsonOptions);

        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue(snapshot.TenantId),
            ["SK"] = new AttributeValue($"INCIDENT#{snapshot.IncidentId}"),
            ["EntityType"] = new AttributeValue("Incident"),
            ["Status"] = new AttributeValue(snapshot.Status.ToString()),
            ["CreatedAt"] = new AttributeValue { S = snapshot.CreatedAt.ToString("O") },
            ["Payload"] = new AttributeValue(payload)
        };

        var request = new PutItemRequest
        {
            TableName = options.Value.IncidentTableName,
            Item = item
        };

        await dynamoDb.PutItemAsync(request, cancellationToken);
    }

    public async Task<Incident?> GetAsync(string tenantId, Guid incidentId, CancellationToken cancellationToken)
    {
        var request = new GetItemRequest
        {
            TableName = options.Value.IncidentTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue(tenantId),
                ["SK"] = new AttributeValue($"INCIDENT#{incidentId}")
            },
            ConsistentRead = true
        };

        var response = await dynamoDb.GetItemAsync(request, cancellationToken);
        if (response.Item is null || response.Item.Count == 0 || !response.Item.TryGetValue("Payload", out var payload))
        {
            return null;
        }

        var snapshot = JsonSerializer.Deserialize<IncidentSnapshot>(payload.S, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize incident snapshot.");

        return Incident.FromSnapshot(snapshot);
    }

    public async Task<IReadOnlyList<Incident>> ListAsync(
        string tenantId,
        IncidentStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var request = new QueryRequest
        {
            TableName = options.Value.IncidentTableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue(tenantId),
                [":sk"] = new AttributeValue("INCIDENT#")
            }
        };

        var response = await dynamoDb.QueryAsync(request, cancellationToken);
        var incidents = new List<Incident>();

        foreach (var item in response.Items)
        {
            if (!item.TryGetValue("Payload", out var payload))
            {
                continue;
            }

            var snapshot = JsonSerializer.Deserialize<IncidentSnapshot>(payload.S, JsonOptions);
            if (snapshot is null)
            {
                continue;
            }

            if (status.HasValue && snapshot.Status != status.Value)
            {
                continue;
            }

            if (from.HasValue && snapshot.CreatedAt < from.Value)
            {
                continue;
            }

            if (to.HasValue && snapshot.CreatedAt > to.Value)
            {
                continue;
            }

            incidents.Add(Incident.FromSnapshot(snapshot));
        }

        return incidents;
    }
}
