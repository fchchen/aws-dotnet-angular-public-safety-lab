using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PublicSafetyLab.Contracts.Incidents;

namespace PublicSafetyLab.Api.Tests.Incidents;

public sealed class IncidentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IncidentApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.WithWebHostBuilder(_ => { }).CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", "demo");
    }

    [Fact]
    public async Task CreateIncident_ThenGetIncident_ShouldReturnCreatedIncident()
    {
        var createRequest = new CreateIncidentRequest(
            Title: "Downtown disturbance",
            Description: "Large crowd and traffic block",
            Priority: "High",
            Location: "2nd Ave",
            ReportedAt: DateTimeOffset.UtcNow);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/incidents", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<IncidentDetailDto>();
        created.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/v1/incidents/{created!.IncidentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loaded = await getResponse.Content.ReadFromJsonAsync<IncidentDetailDto>();
        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Downtown disturbance");
        loaded.Status.Should().Be(IncidentStatus.New);
    }

    [Fact]
    public async Task QueueProcessing_ShouldReturnAccepted()
    {
        var createRequest = new CreateIncidentRequest(
            Title: "Signal outage",
            Description: "Signal down at intersection",
            Priority: "Medium",
            Location: "Maple Rd",
            ReportedAt: DateTimeOffset.UtcNow);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/incidents", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<IncidentDetailDto>();
        created.Should().NotBeNull();

        var processResponse = await _client.PostAsJsonAsync(
            $"/api/v1/incidents/{created!.IncidentId}/process",
            new QueueIncidentProcessingRequest("Manual review"));

        processResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var getResponse = await _client.GetAsync($"/api/v1/incidents/{created.IncidentId}");
        var loaded = await getResponse.Content.ReadFromJsonAsync<IncidentDetailDto>();

        loaded!.Status.Should().Be(IncidentStatus.Queued);
    }

    [Fact]
    public async Task CreateIncident_WithoutApiKey_InStrictMode_ShouldReturnUnauthorized()
    {
        var strictFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:AllowLegacyTenantHeader"] = "false"
                });
            }));

        var client = strictFactory.CreateClient();

        var request = new CreateIncidentRequest(
            Title: "Unauthorized request",
            Description: "No API key should be rejected",
            Priority: "High",
            Location: "1st St",
            ReportedAt: DateTimeOffset.UtcNow);

        var response = await client.PostAsJsonAsync("/api/v1/incidents", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
