using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Infrastructure.Configuration;
using PublicSafetyLab.Infrastructure.Evidence;
using PublicSafetyLab.Infrastructure.Incidents;
using PublicSafetyLab.Infrastructure.Queue;

namespace PublicSafetyLab.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublicSafetyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsResourceOptions>(configuration.GetSection(AwsResourceOptions.SectionName));

        var options = configuration.GetSection(AwsResourceOptions.SectionName).Get<AwsResourceOptions>() ?? new AwsResourceOptions();

        if (!options.UseAws)
        {
            services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
            services.AddSingleton<IEvidenceStorageService, LocalEvidenceStorageService>();
            services.AddSingleton<InMemoryIncidentQueueClient>();
            services.AddSingleton<IIncidentQueuePublisher>(provider => provider.GetRequiredService<InMemoryIncidentQueueClient>());
            services.AddSingleton<IIncidentQueueConsumer>(provider => provider.GetRequiredService<InMemoryIncidentQueueClient>());
            return services;
        }

        services.AddSingleton<IAmazonDynamoDB>(provider =>
        {
            var resourceOptions = provider.GetRequiredService<IOptions<AwsResourceOptions>>().Value;
            return BuildDynamoDbClient(resourceOptions);
        });

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var resourceOptions = provider.GetRequiredService<IOptions<AwsResourceOptions>>().Value;
            return BuildS3Client(resourceOptions);
        });

        services.AddSingleton<IAmazonSQS>(provider =>
        {
            var resourceOptions = provider.GetRequiredService<IOptions<AwsResourceOptions>>().Value;
            return BuildSqsClient(resourceOptions);
        });

        services.AddSingleton<IIncidentRepository, DynamoDbIncidentRepository>();
        services.AddSingleton<IEvidenceStorageService, S3EvidenceStorageService>();
        services.AddSingleton<SqsIncidentQueueClient>();
        services.AddSingleton<IIncidentQueuePublisher>(provider => provider.GetRequiredService<SqsIncidentQueueClient>());
        services.AddSingleton<IIncidentQueueConsumer>(provider => provider.GetRequiredService<SqsIncidentQueueClient>());

        return services;
    }

    private static IAmazonDynamoDB BuildDynamoDbClient(AwsResourceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            return new AmazonDynamoDBClient(
                new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                new AmazonDynamoDBConfig { ServiceURL = options.ServiceUrl });
        }

        return new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(options.Region));
    }

    private static IAmazonS3 BuildS3Client(AwsResourceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            return new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = options.ServiceUrl,
                    ForcePathStyle = true
                });
        }

        return new AmazonS3Client(RegionEndpoint.GetBySystemName(options.Region));
    }

    private static IAmazonSQS BuildSqsClient(AwsResourceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            return new AmazonSQSClient(
                new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                new AmazonSQSConfig { ServiceURL = options.ServiceUrl });
        }

        return new AmazonSQSClient(RegionEndpoint.GetBySystemName(options.Region));
    }
}
