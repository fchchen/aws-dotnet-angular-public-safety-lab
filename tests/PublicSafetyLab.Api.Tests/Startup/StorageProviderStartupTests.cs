using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Infrastructure.DependencyInjection;
using PublicSafetyLab.Infrastructure.Incidents;

namespace PublicSafetyLab.Api.Tests.Startup;

public sealed class StorageProviderStartupTests
{
    [Fact]
    public void StorageProvider_PostgreSql_ShouldResolvePostgreSqlRepository()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AwsResources:UseAws"] = "false",
                ["AwsResources:StorageProvider"] = "PostgreSql",
                ["AwsResources:PostgreSqlConnectionString"] =
                    "Host=localhost;Port=5432;Database=public_safety_lab;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPublicSafetyInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIncidentRepository>();

        repository.Should().BeOfType<PostgreSqlIncidentRepository>();
    }
}
