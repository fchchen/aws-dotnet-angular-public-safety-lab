using System.Collections.Concurrent;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Queue;

namespace PublicSafetyLab.Infrastructure.Queue;

public sealed class InMemoryIncidentQueueClient : IIncidentQueuePublisher, IIncidentQueueConsumer
{
    private readonly ConcurrentDictionary<string, IncidentProcessingMessage> _messages = new();

    public Task PublishAsync(IncidentProcessingMessage message, CancellationToken cancellationToken)
    {
        var receiptHandle = Guid.NewGuid().ToString("N");
        _messages.TryAdd(receiptHandle, message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IncidentQueueEnvelope>> ReceiveAsync(int maxMessages, CancellationToken cancellationToken)
    {
        var result = _messages
            .Take(Math.Max(maxMessages, 1))
            .Select(x => new IncidentQueueEnvelope(x.Key, x.Value))
            .ToArray();

        return Task.FromResult<IReadOnlyList<IncidentQueueEnvelope>>(result);
    }

    public Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken)
    {
        _messages.TryRemove(receiptHandle, out _);
        return Task.CompletedTask;
    }
}
