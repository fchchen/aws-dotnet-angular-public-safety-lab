using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Application.Common;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Infrastructure.Configuration;
using PublicSafetyLab.Infrastructure.Evidence;
using PublicSafetyLab.Infrastructure.HealthChecks;
using PublicSafetyLab.Infrastructure.Incidents;
using PublicSafetyLab.Infrastructure.Persistence;
using PublicSafetyLab.Infrastructure.Queue;

namespace PublicSafetyLab.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublicSafetyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsResourceOptions>(configuration.GetSection(AwsResourceOptions.SectionName));
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<IncidentService>();

        var options = configuration.GetSection(AwsResourceOptions.SectionName).Get<AwsResourceOptions>() ?? new AwsResourceOptions();
        var storageProvider = ResolveStorageProvider(options);

        switch (storageProvider.ToLowerInvariant())
        {
            case "inmemory":
                services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
                services.AddSingleton<IEvidenceStorageService, LocalEvidenceStorageService>();
                services.AddSingleton<InMemoryIncidentQueueClient>();
                services.AddSingleton<IIncidentQueuePublisher>(provider => provider.GetRequiredService<InMemoryIncidentQueueClient>());
                services.AddSingleton<IIncidentQueueConsumer>(provider => provider.GetRequiredService<InMemoryIncidentQueueClient>());
                return services;

            case "dynamodb":
                RegisterAwsClients(services);
                services.AddSingleton<IIncidentRepository, DynamoDbIncidentRepository>();
                services.AddSingleton<IEvidenceStorageService, S3EvidenceStorageService>();
                services.AddSingleton<SqsIncidentQueueClient>();
                services.AddSingleton<IIncidentQueuePublisher>(provider => provider.GetRequiredService<SqsIncidentQueueClient>());
                services.AddSingleton<IIncidentQueueConsumer>(provider => provider.GetRequiredService<SqsIncidentQueueClient>());
                return services;

            case "postgresql":
                if (string.IsNullOrWhiteSpace(options.PostgreSqlConnectionString))
                {
                    throw new InvalidOperationException("AwsResources:PostgreSqlConnectionString must be configured when StorageProvider is PostgreSql.");
                }

                services.AddDbContext<PublicSafetyDbContext>(dbOptions =>
                    dbOptions.UseNpgsql(options.PostgreSqlConnectionString));

                services.AddScoped<IIncidentRepository, PostgreSqlIncidentRepository>();

                if (options.UseAws)
                {
                    RegisterAwsClients(services);
                    services.AddSingleton<IEvidenceStorageService, S3EvidenceStorageService>();
                    services.AddSingleton<SqsIncidentQueueClient>();
                    services.AddSingleton<IIncidentQueuePublisher>(provider => provider.GetRequiredService<SqsIncidentQueueClient>());
                    services.AddSingleton<IIncidentQueueConsumer>(provider => provider.GetRequiredService<SqsIncidentQueueClient>());
                }
                else
                {
                    services.AddSingleton<IEvidenceStorageService, LocalEvidenceStorageService>();
                    services.AddSingleton<InMemoryIncidentQueueClient>();
                    services.AddSingleton<IIncidentQueuePublisher>(provider => provider.GetRequiredService<InMemoryIncidentQueueClient>());
                    services.AddSingleton<IIncidentQueueConsumer>(provider => provider.GetRequiredService<InMemoryIncidentQueueClient>());
                }

                return services;

            default:
                throw new InvalidOperationException(
                    $"AwsResources:StorageProvider value '{storageProvider}' is invalid. Expected InMemory, DynamoDb, or PostgreSql.");
        }
    }

    public static IServiceCollection AddPublicSafetyHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AwsResourceOptions.SectionName).Get<AwsResourceOptions>() ?? new AwsResourceOptions();
        var storageProvider = ResolveStorageProvider(options).ToLowerInvariant();
        var healthChecks = services.AddHealthChecks();

        switch (storageProvider)
        {
            case "inmemory":
                break;

            case "dynamodb":
                healthChecks.AddCheck<DynamoDbHealthCheck>("dynamodb", tags: ["ready"]);
                healthChecks.AddCheck<S3HealthCheck>("s3", tags: ["ready"]);
                healthChecks.AddCheck<SqsHealthCheck>("sqs", tags: ["ready"]);
                break;

            case "postgresql":
                healthChecks.AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"]);
                if (options.UseAws)
                {
                    healthChecks.AddCheck<S3HealthCheck>("s3", tags: ["ready"]);
                    healthChecks.AddCheck<SqsHealthCheck>("sqs", tags: ["ready"]);
                }
                break;

            default:
                throw new InvalidOperationException(
                    $"AwsResources:StorageProvider value '{storageProvider}' is invalid. Expected InMemory, DynamoDb, or PostgreSql.");
        }

        return services;
    }

    private static void RegisterAwsClients(IServiceCollection services)
    {
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
    }

    public static string ResolveStorageProvider(AwsResourceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.StorageProvider))
        {
            return options.StorageProvider.Trim();
        }

        return options.UseAws ? "DynamoDb" : "InMemory";
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
