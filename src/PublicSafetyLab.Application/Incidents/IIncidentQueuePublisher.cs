using PublicSafetyLab.Contracts.Queue;

namespace PublicSafetyLab.Application.Incidents;

public interface IIncidentQueuePublisher
{
    Task PublishAsync(IncidentProcessingMessage message, CancellationToken cancellationToken);
}

public sealed record IncidentQueueEnvelope(
    string ReceiptHandle,
    IncidentProcessingMessage Message);

public interface IIncidentQueueConsumer
{
    Task<IReadOnlyList<IncidentQueueEnvelope>> ReceiveAsync(int maxMessages, CancellationToken cancellationToken);

    Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken);
}
