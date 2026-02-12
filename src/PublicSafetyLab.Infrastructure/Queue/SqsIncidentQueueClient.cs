using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Queue;
using PublicSafetyLab.Infrastructure.Configuration;

namespace PublicSafetyLab.Infrastructure.Queue;

public sealed class SqsIncidentQueueClient(
    IAmazonSQS sqs,
    IOptions<AwsResourceOptions> options) : IIncidentQueuePublisher, IIncidentQueueConsumer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(IncidentProcessingMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.IncidentQueueUrl))
        {
            throw new InvalidOperationException("AwsResources:IncidentQueueUrl must be configured.");
        }

        var payload = JsonSerializer.Serialize(message, JsonOptions);
        var request = new SendMessageRequest
        {
            QueueUrl = options.Value.IncidentQueueUrl,
            MessageBody = payload
        };

        await sqs.SendMessageAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentQueueEnvelope>> ReceiveAsync(int maxMessages, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.IncidentQueueUrl))
        {
            return [];
        }

        var request = new ReceiveMessageRequest
        {
            QueueUrl = options.Value.IncidentQueueUrl,
            MaxNumberOfMessages = Math.Clamp(maxMessages, 1, 10),
            WaitTimeSeconds = 1
        };

        var response = await sqs.ReceiveMessageAsync(request, cancellationToken);
        var envelopes = new List<IncidentQueueEnvelope>();
        var messages = response?.Messages ?? [];

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Body) || string.IsNullOrWhiteSpace(message.ReceiptHandle))
            {
                continue;
            }

            IncidentProcessingMessage? deserialized;
            try
            {
                deserialized = JsonSerializer.Deserialize<IncidentProcessingMessage>(message.Body, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (deserialized is null)
            {
                continue;
            }

            envelopes.Add(new IncidentQueueEnvelope(message.ReceiptHandle, deserialized));
        }

        return envelopes;
    }

    public Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.IncidentQueueUrl))
        {
            return Task.CompletedTask;
        }

        var request = new DeleteMessageRequest
        {
            QueueUrl = options.Value.IncidentQueueUrl,
            ReceiptHandle = receiptHandle
        };

        return sqs.DeleteMessageAsync(request, cancellationToken);
    }
}
