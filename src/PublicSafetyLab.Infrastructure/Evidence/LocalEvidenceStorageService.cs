using PublicSafetyLab.Application.Incidents;

namespace PublicSafetyLab.Infrastructure.Evidence;

public sealed class LocalEvidenceStorageService : IEvidenceStorageService
{
    public Task<PresignedEvidenceUpload> CreateUploadUrlAsync(
        string tenantId,
        Guid incidentId,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var sanitizedName = Path.GetFileName(fileName);
        var objectKey = $"tenant/{tenantId}/incident/{incidentId}/evidence/{Guid.NewGuid():N}-{sanitizedName}";

        var upload = new PresignedEvidenceUpload(
            UploadUrl: $"https://local-upload.invalid/{objectKey}",
            ObjectKey: objectKey,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15));

        return Task.FromResult(upload);
    }
}
