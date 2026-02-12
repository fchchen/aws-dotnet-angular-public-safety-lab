using Microsoft.Extensions.Logging;
using PublicSafetyLab.Application.Common;
using PublicSafetyLab.Application.Exceptions;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Contracts.Queue;
using PublicSafetyLab.Domain.Incidents;

namespace PublicSafetyLab.Application.Incidents;

public sealed class IncidentService(
    IIncidentRepository repository,
    IEvidenceStorageService evidenceStorageService,
    IIncidentQueuePublisher queuePublisher,
    IClock clock,
    ILogger<IncidentService> logger)
{
    public async Task<IncidentDetailDto> CreateIncidentAsync(
        string tenantId,
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var incident = Incident.Create(
            tenantId,
            request.Title,
            request.Description,
            request.Priority,
            request.Location,
            request.ReportedAt,
            clock.UtcNow);

        await repository.SaveAsync(incident, cancellationToken);
        logger.LogInformation("Created incident {IncidentId} for tenant {TenantId}", incident.IncidentId, tenantId);

        return MapDetail(incident);
    }

    public async Task<IReadOnlyList<IncidentSummaryDto>> ListIncidentsAsync(
        string tenantId,
        IncidentStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var incidents = await repository.ListAsync(tenantId, status, from, to, cancellationToken);

        return incidents
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapSummary)
            .ToArray();
    }

    public async Task<IncidentDetailDto> GetIncidentAsync(
        string tenantId,
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var incident = await repository.GetAsync(tenantId, incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident {incidentId} was not found for tenant {tenantId}.");

        return MapDetail(incident);
    }

    public async Task<UploadEvidenceUrlResponse> CreateEvidenceUploadUrlAsync(
        string tenantId,
        Guid incidentId,
        UploadEvidenceUrlRequest request,
        CancellationToken cancellationToken)
    {
        var incident = await repository.GetAsync(tenantId, incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident {incidentId} was not found for tenant {tenantId}.");

        var upload = await evidenceStorageService.CreateUploadUrlAsync(
            tenantId,
            incidentId,
            request.FileName,
            request.ContentType,
            cancellationToken);

        incident.AddEvidence(request.FileName, upload.ObjectKey, clock.UtcNow);
        await repository.SaveAsync(incident, cancellationToken);

        return new UploadEvidenceUrlResponse(upload.UploadUrl, upload.ObjectKey, upload.ExpiresAt);
    }

    public async Task QueueIncidentProcessingAsync(
        string tenantId,
        Guid incidentId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var incident = await repository.GetAsync(tenantId, incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident {incidentId} was not found for tenant {tenantId}.");

        incident.MarkQueued(clock.UtcNow);
        await repository.SaveAsync(incident, cancellationToken);

        var message = new IncidentProcessingMessage(
            MessageType: "IncidentProcessingRequested",
            TenantId: tenantId,
            IncidentId: incidentId,
            CorrelationId: Guid.NewGuid().ToString("N"),
            OccurredAt: clock.UtcNow,
            Reason: reason);

        await queuePublisher.PublishAsync(message, cancellationToken);
    }

    public async Task ProcessIncidentAsync(IncidentProcessingMessage message, CancellationToken cancellationToken)
    {
        var incident = await repository.GetAsync(message.TenantId, message.IncidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident {message.IncidentId} was not found for tenant {message.TenantId}.");

        // Simulates async validation/enrichment work.
        if (incident.Evidence.Count == 0)
        {
            incident.MarkFailed("No evidence attached.", clock.UtcNow);
        }
        else
        {
            incident.MarkProcessed(clock.UtcNow);
        }

        await repository.SaveAsync(incident, cancellationToken);
    }

    private static IncidentSummaryDto MapSummary(Incident incident)
    {
        return new IncidentSummaryDto(
            incident.IncidentId,
            incident.Title,
            incident.Priority,
            incident.Location,
            incident.Status,
            incident.CreatedAt,
            incident.ReportedAt);
    }

    private static IncidentDetailDto MapDetail(Incident incident)
    {
        return new IncidentDetailDto(
            incident.IncidentId,
            incident.TenantId,
            incident.Title,
            incident.Description,
            incident.Priority,
            incident.Location,
            incident.Status,
            incident.CreatedAt,
            incident.ReportedAt,
            incident.QueuedAt,
            incident.ProcessedAt,
            incident.FailureReason,
            incident.Evidence.Select(x => new EvidenceDto(x.FileName, x.ObjectKey, x.UploadedAt)).ToArray());
    }
}
