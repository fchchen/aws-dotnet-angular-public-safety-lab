using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Infrastructure.Configuration;

namespace PublicSafetyLab.Infrastructure.Evidence;

public sealed class S3EvidenceStorageService(
    IAmazonS3 s3,
    IOptions<AwsResourceOptions> options) : IEvidenceStorageService
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
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.EvidenceUploadExpiryMinutes);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.Value.EvidenceBucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType
        };

        var uploadUrl = s3.GetPreSignedURL(request);
        return Task.FromResult(new PresignedEvidenceUpload(uploadUrl, objectKey, expiresAt));
    }
}
