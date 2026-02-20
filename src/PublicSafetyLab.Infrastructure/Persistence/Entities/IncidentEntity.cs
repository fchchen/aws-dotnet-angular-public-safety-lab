using PublicSafetyLab.Contracts.Incidents;

namespace PublicSafetyLab.Infrastructure.Persistence.Entities;

public sealed class IncidentEntity
{
    public Guid Id { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public IncidentStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ReportedAt { get; set; }

    public DateTimeOffset? QueuedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? FailureReason { get; set; }

    public int Version { get; set; }

    public List<EvidenceItemEntity> EvidenceItems { get; set; } = [];
}
