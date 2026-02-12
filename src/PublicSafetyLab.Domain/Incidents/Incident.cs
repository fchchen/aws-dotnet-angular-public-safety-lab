using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Exceptions;

namespace PublicSafetyLab.Domain.Incidents;

public sealed class Incident
{
    private static readonly HashSet<string> AllowedPriorities =
    [
        "Low",
        "Medium",
        "High",
        "Critical"
    ];

    private readonly List<EvidenceItem> _evidence;

    private Incident(
        Guid incidentId,
        string tenantId,
        string title,
        string description,
        string priority,
        string location,
        IncidentStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset reportedAt,
        DateTimeOffset? queuedAt,
        DateTimeOffset? processedAt,
        string? failureReason,
        IEnumerable<EvidenceItem> evidence,
        int version)
    {
        IncidentId = incidentId;
        TenantId = tenantId;
        Title = title;
        Description = description;
        Priority = priority;
        Location = location;
        Status = status;
        CreatedAt = createdAt;
        ReportedAt = reportedAt;
        QueuedAt = queuedAt;
        ProcessedAt = processedAt;
        FailureReason = failureReason;
        _evidence = [.. evidence];
        Version = version;
    }

    public Guid IncidentId { get; }

    public string TenantId { get; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public string Priority { get; private set; }

    public string Location { get; private set; }

    public IncidentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ReportedAt { get; }

    public DateTimeOffset? QueuedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<EvidenceItem> Evidence => _evidence.AsReadOnly();

    public static Incident Create(
        string tenantId,
        string title,
        string description,
        string priority,
        string location,
        DateTimeOffset reportedAt,
        DateTimeOffset createdAt)
    {
        ValidateTenant(tenantId);
        ValidateTitle(title);
        ValidatePriority(priority);
        ValidateLocation(location);

        return new Incident(
            Guid.NewGuid(),
            tenantId.Trim(),
            title.Trim(),
            description.Trim(),
            priority.Trim(),
            location.Trim(),
            IncidentStatus.New,
            createdAt,
            reportedAt,
            null,
            null,
            null,
            [],
            1);
    }

    public static Incident FromSnapshot(IncidentSnapshot snapshot)
    {
        return new Incident(
            snapshot.IncidentId,
            snapshot.TenantId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Priority,
            snapshot.Location,
            snapshot.Status,
            snapshot.CreatedAt,
            snapshot.ReportedAt,
            snapshot.QueuedAt,
            snapshot.ProcessedAt,
            snapshot.FailureReason,
            snapshot.Evidence,
            snapshot.Version);
    }

    public void AddEvidence(string fileName, string objectKey, DateTimeOffset uploadedAt)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainValidationException("Evidence file name is required.");
        }

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new DomainValidationException("Evidence object key is required.");
        }

        _evidence.Add(new EvidenceItem(fileName.Trim(), objectKey.Trim(), uploadedAt));
        Version++;
    }

    public void MarkQueued(DateTimeOffset queuedAt)
    {
        if (Status is IncidentStatus.Processed)
        {
            throw new DomainValidationException("A processed incident cannot be queued again.");
        }

        Status = IncidentStatus.Queued;
        QueuedAt = queuedAt;
        FailureReason = null;
        Version++;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (Status is not IncidentStatus.Queued)
        {
            throw new DomainValidationException("Only queued incidents can be processed.");
        }

        Status = IncidentStatus.Processed;
        ProcessedAt = processedAt;
        FailureReason = null;
        Version++;
    }

    public void MarkFailed(string reason, DateTimeOffset failedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("A failure reason is required.");
        }

        Status = IncidentStatus.Failed;
        ProcessedAt = failedAt;
        FailureReason = reason.Trim();
        Version++;
    }

    public IncidentSnapshot ToSnapshot()
    {
        return new IncidentSnapshot(
            IncidentId,
            TenantId,
            Title,
            Description,
            Priority,
            Location,
            Status,
            CreatedAt,
            ReportedAt,
            QueuedAt,
            ProcessedAt,
            FailureReason,
            Evidence,
            Version);
    }

    private static void ValidateTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new DomainValidationException("Tenant id is required.");
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Incident title is required.");
        }
    }

    private static void ValidatePriority(string priority)
    {
        if (string.IsNullOrWhiteSpace(priority) || !AllowedPriorities.Contains(priority.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Priority must be one of: Low, Medium, High, Critical.");
        }
    }

    private static void ValidateLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new DomainValidationException("Location is required.");
        }
    }
}
