using PublicSafetyLab.Contracts.Incidents;

namespace PublicSafetyLab.Domain.Incidents;

public sealed record EvidenceItem(
    string FileName,
    string ObjectKey,
    DateTimeOffset UploadedAt);

public sealed record IncidentSnapshot(
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
    IReadOnlyList<EvidenceItem> Evidence,
    int Version);
