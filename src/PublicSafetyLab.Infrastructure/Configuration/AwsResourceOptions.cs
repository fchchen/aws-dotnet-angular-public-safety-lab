namespace PublicSafetyLab.Infrastructure.Configuration;

public sealed class AwsResourceOptions
{
    public const string SectionName = "AwsResources";

    public bool UseAws { get; set; }

    public string? StorageProvider { get; set; }

    public string? PostgreSqlConnectionString { get; set; }

    public string Region { get; set; } = "us-east-1";

    public string? ServiceUrl { get; set; }

    public string AccessKeyId { get; set; } = "test";

    public string SecretAccessKey { get; set; } = "test";

    public string IncidentTableName { get; set; } = "incident_items";

    public string EvidenceBucketName { get; set; } = "public-safety-lab-evidence-dev";

    public string IncidentQueueUrl { get; set; } = "http://localhost:4566/000000000000/incident-events-dev";

    public int EvidenceUploadExpiryMinutes { get; set; } = 15;
}
