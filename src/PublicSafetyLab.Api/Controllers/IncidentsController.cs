using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Api.Authentication;
using PublicSafetyLab.Api.Middleware;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Incidents;

namespace PublicSafetyLab.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/incidents")]
public sealed class IncidentsController(
    IncidentService incidentService,
    IOptions<ApiKeyAuthenticationOptions> authOptions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IncidentDetailDto>> CreateIncident(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryResolveTenantId(out var tenantId))
        {
            return Unauthorized();
        }

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
        if (!TryResolveTenantId(out var tenantId))
        {
            return Unauthorized();
        }

        var incidents = await incidentService.ListIncidentsAsync(tenantId, status, from, to, cancellationToken);

        return Ok(incidents);
    }

    [HttpGet("{incidentId:guid}")]
    public async Task<ActionResult<IncidentDetailDto>> GetIncidentById(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveTenantId(out var tenantId))
        {
            return Unauthorized();
        }

        var incident = await incidentService.GetIncidentAsync(tenantId, incidentId, cancellationToken);

        return Ok(incident);
    }

    [HttpPost("{incidentId:guid}/evidence/upload-url")]
    public async Task<ActionResult<UploadEvidenceUrlResponse>> CreateEvidenceUploadUrl(
        Guid incidentId,
        [FromBody] UploadEvidenceUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryResolveTenantId(out var tenantId))
        {
            return Unauthorized();
        }

        var upload = await incidentService.CreateEvidenceUploadUrlAsync(tenantId, incidentId, request, cancellationToken);

        return Ok(upload);
    }

    [HttpPost("{incidentId:guid}/process")]
    public async Task<ActionResult> QueueProcessing(
        Guid incidentId,
        [FromBody] QueueIncidentProcessingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryResolveTenantId(out var tenantId))
        {
            return Unauthorized();
        }

        await incidentService.QueueIncidentProcessingAsync(
            tenantId,
            incidentId,
            request.Reason,
            cancellationToken,
            ResolveCorrelationId());
        return Accepted();
    }

    private bool TryResolveTenantId(out string tenantId)
    {
        var claimTenantId = User.FindFirst(ApiKeyAuthenticationHandler.TenantIdClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(claimTenantId))
        {
            tenantId = claimTenantId.Trim();
            return true;
        }

        if (authOptions.Value.AllowLegacyTenantHeader &&
            Request.Headers.TryGetValue(ApiKeyAuthenticationHandler.LegacyTenantHeaderName, out var legacyTenantHeader) &&
            !string.IsNullOrWhiteSpace(legacyTenantHeader))
        {
            tenantId = legacyTenantHeader.ToString().Trim();
            return true;
        }

        tenantId = string.Empty;
        return false;
    }

    private string ResolveCorrelationId()
    {
        if (HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) &&
            value is string fromItems &&
            !string.IsNullOrWhiteSpace(fromItems))
        {
            return fromItems;
        }

        if (Request.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var header) &&
            !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString().Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
