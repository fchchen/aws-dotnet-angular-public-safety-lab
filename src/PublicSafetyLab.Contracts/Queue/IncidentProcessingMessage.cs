namespace PublicSafetyLab.Contracts.Queue;

public sealed record IncidentProcessingMessage(
    string MessageType,
    string TenantId,
    Guid IncidentId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string? Reason);
