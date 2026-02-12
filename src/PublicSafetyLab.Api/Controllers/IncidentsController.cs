using Microsoft.AspNetCore.Mvc;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Incidents;

namespace PublicSafetyLab.Api.Controllers;

[ApiController]
[Route("api/v1/incidents")]
public sealed class IncidentsController(IncidentService incidentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IncidentDetailDto>> CreateIncident(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        var created = await incidentService.CreateIncidentAsync(tenantId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetIncidentById),
            new { incidentId = created.IncidentId },
            created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentSummaryDto>>> ListIncidents(
        [FromQuery] IncidentStatus? status,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        var incidents = await incidentService.ListIncidentsAsync(tenantId, status, from, to, cancellationToken);

        return Ok(incidents);
    }

    [HttpGet("{incidentId:guid}")]
    public async Task<ActionResult<IncidentDetailDto>> GetIncidentById(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        var incident = await incidentService.GetIncidentAsync(tenantId, incidentId, cancellationToken);

        return Ok(incident);
    }

    [HttpPost("{incidentId:guid}/evidence/upload-url")]
    public async Task<ActionResult<UploadEvidenceUrlResponse>> CreateEvidenceUploadUrl(
        Guid incidentId,
        [FromBody] UploadEvidenceUrlRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        var upload = await incidentService.CreateEvidenceUploadUrlAsync(tenantId, incidentId, request, cancellationToken);

        return Ok(upload);
    }

    [HttpPost("{incidentId:guid}/process")]
    public async Task<ActionResult> QueueProcessing(
        Guid incidentId,
        [FromBody] QueueIncidentProcessingRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        await incidentService.QueueIncidentProcessingAsync(tenantId, incidentId, request.Reason, cancellationToken);
        return Accepted();
    }

    private string ResolveTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId.ToString().Trim();
        }

        return "demo";
    }
}
