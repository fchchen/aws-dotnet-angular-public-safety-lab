namespace PublicSafetyLab.Infrastructure.Persistence.Entities;

public sealed class EvidenceItemEntity
{
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ObjectKey { get; set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; set; }

    public int SortOrder { get; set; }

    public IncidentEntity Incident { get; set; } = null!;
}
