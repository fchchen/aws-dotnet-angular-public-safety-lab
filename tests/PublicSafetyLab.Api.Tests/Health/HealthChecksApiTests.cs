using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PublicSafetyLab.Infrastructure.DependencyInjection;

namespace PublicSafetyLab.Api.Tests.Health;

public sealed class HealthChecksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthChecksApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveHealthCheck_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/healthz/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyHealthCheck_WithInMemoryProvider_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/healthz/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyHealthCheck_WithUnreachablePostgreSql_ShouldReturnUnhealthy()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AwsResources:UseAws"] = "false",
                ["AwsResources:StorageProvider"] = "PostgreSql",
                ["AwsResources:PostgreSqlConnectionString"] =
                    "Host=127.0.0.1;Port=65432;Database=public_safety_lab;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPublicSafetyInfrastructure(configuration);
        services.AddPublicSafetyHealthChecks(configuration);

        using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));

        report.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
