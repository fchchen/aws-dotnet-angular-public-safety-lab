using Microsoft.Extensions.DependencyInjection;
using PublicSafetyLab.Application.Incidents;
using Serilog.Context;

namespace PublicSafetyLab.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<IncidentQueueEnvelope> messages;
            try
            {
                using var receiveScope = scopeFactory.CreateScope();
                var queueConsumer = receiveScope.ServiceProvider.GetRequiredService<IIncidentQueueConsumer>();
                messages = await queueConsumer.ReceiveAsync(maxMessages: 5, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to receive messages from incident queue.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            foreach (var envelope in messages)
            {
                using var messageScope = scopeFactory.CreateScope();
                var queueConsumer = messageScope.ServiceProvider.GetRequiredService<IIncidentQueueConsumer>();
                var incidentService = messageScope.ServiceProvider.GetRequiredService<IncidentService>();

                using (LogContext.PushProperty("CorrelationId", envelope.Message.CorrelationId))
                using (LogContext.PushProperty("TenantId", envelope.Message.TenantId))
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
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
