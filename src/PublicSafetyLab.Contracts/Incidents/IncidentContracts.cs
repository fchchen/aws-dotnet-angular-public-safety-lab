namespace PublicSafetyLab.Contracts.Incidents;

public enum IncidentStatus
{
    New,
    Queued,
    Processed,
    Failed
}

public sealed record CreateIncidentRequest(
    string Title,
    string Description,
    string Priority,
    string Location,
    DateTimeOffset ReportedAt);

public sealed record IncidentSummaryDto(
    Guid IncidentId,
    string Title,
    string Priority,
    string Location,
    IncidentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ReportedAt);

public sealed record EvidenceDto(
    string FileName,
    string ObjectKey,
    DateTimeOffset UploadedAt);

public sealed record IncidentDetailDto(
    Guid IncidentId,
    string TenantId,
    string Title,
    string Description,
    string Priority,
    string Location,
    IncidentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ReportedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? ProcessedAt,
    string? FailureReason,
    IReadOnlyList<EvidenceDto> Evidence);

public sealed record UploadEvidenceUrlRequest(
    string FileName,
    string ContentType);

public sealed record UploadEvidenceUrlResponse(
    string UploadUrl,
    string ObjectKey,
    DateTimeOffset ExpiresAt);

public sealed record QueueIncidentProcessingRequest(
    string? Reason);
