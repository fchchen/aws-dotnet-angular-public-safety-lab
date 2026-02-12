using PublicSafetyLab.Application.Incidents;

namespace PublicSafetyLab.Worker;

public sealed class Worker(
    IIncidentQueueConsumer queueConsumer,
    IncidentService incidentService,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await queueConsumer.ReceiveAsync(maxMessages: 5, stoppingToken);

            foreach (var envelope in messages)
            {
                try
                {
                    await incidentService.ProcessIncidentAsync(envelope.Message, stoppingToken);
                    await queueConsumer.DeleteAsync(envelope.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to process incident message {IncidentId} with correlation {CorrelationId}",
                        envelope.Message.IncidentId,
                        envelope.Message.CorrelationId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
