namespace PublicSafetyLab.Application.Incidents;

public sealed record PresignedEvidenceUpload(
    string UploadUrl,
    string ObjectKey,
    DateTimeOffset ExpiresAt);

public interface IEvidenceStorageService
{
    Task<PresignedEvidenceUpload> CreateUploadUrlAsync(
        string tenantId,
        Guid incidentId,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
