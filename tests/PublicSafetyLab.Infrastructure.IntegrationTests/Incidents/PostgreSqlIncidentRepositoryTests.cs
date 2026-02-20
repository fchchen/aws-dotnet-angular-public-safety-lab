using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Incidents;
using PublicSafetyLab.Infrastructure.Incidents;
using PublicSafetyLab.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PublicSafetyLab.Infrastructure.IntegrationTests.Incidents;

public sealed class PostgreSqlIncidentRepositoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task SaveAsync_ThenGetAsync_ShouldRoundTripIncidentIncludingEvidence()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var incident = CreateIncident("tenant-a", createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        incident.AddEvidence("photo.jpg", "tenant-a/photo.jpg", DateTimeOffset.UtcNow);

        await repository.SaveAsync(incident, CancellationToken.None);
        var loaded = await repository.GetAsync("tenant-a", incident.IncidentId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.IncidentId.Should().Be(incident.IncidentId);
        loaded.TenantId.Should().Be("tenant-a");
        loaded.Title.Should().Be(incident.Title);
        loaded.Evidence.Should().ContainSingle();
        loaded.Evidence[0].ObjectKey.Should().Be("tenant-a/photo.jpg");
    }

    [Fact]
    public async Task ListAsync_ShouldFilterByStatus()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var queued = CreateIncident("tenant-a", DateTimeOffset.UtcNow.AddHours(-2));
        queued.MarkQueued(DateTimeOffset.UtcNow.AddHours(-1));

        var failed = CreateIncident("tenant-a", DateTimeOffset.UtcNow.AddHours(-3));
        failed.MarkFailed("Validation failed", DateTimeOffset.UtcNow.AddHours(-2));

        var fresh = CreateIncident("tenant-a", DateTimeOffset.UtcNow.AddMinutes(-30));

        await repository.SaveAsync(queued, CancellationToken.None);
        await repository.SaveAsync(failed, CancellationToken.None);
        await repository.SaveAsync(fresh, CancellationToken.None);

        var queuedIncidents = await repository.ListAsync(
            "tenant-a",
            IncidentStatus.Queued,
            from: null,
            to: null,
            CancellationToken.None);

        queuedIncidents.Should().ContainSingle();
        queuedIncidents[0].IncidentId.Should().Be(queued.IncidentId);
        queuedIncidents[0].Status.Should().Be(IncidentStatus.Queued);
    }

    [Fact]
    public async Task ListAsync_ShouldFilterByCreatedAtRange()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var now = DateTimeOffset.UtcNow;
        var oldIncident = CreateIncident("tenant-a", now.AddDays(-2));
        var middleIncident = CreateIncident("tenant-a", now.AddDays(-1));
        var newIncident = CreateIncident("tenant-a", now);

        await repository.SaveAsync(oldIncident, CancellationToken.None);
        await repository.SaveAsync(middleIncident, CancellationToken.None);
        await repository.SaveAsync(newIncident, CancellationToken.None);

        var list = await repository.ListAsync(
            "tenant-a",
            status: null,
            from: now.AddDays(-1.5),
            to: now.AddHours(-12),
            CancellationToken.None);

        list.Should().ContainSingle();
        list[0].IncidentId.Should().Be(middleIncident.IncidentId);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnNewestIncidentsFirst()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var now = DateTimeOffset.UtcNow;
        var oldestIncident = CreateIncident("tenant-a", now.AddHours(-3));
        var newestIncident = CreateIncident("tenant-a", now.AddHours(-1));
        var middleIncident = CreateIncident("tenant-a", now.AddHours(-2));

        await repository.SaveAsync(oldestIncident, CancellationToken.None);
        await repository.SaveAsync(newestIncident, CancellationToken.None);
        await repository.SaveAsync(middleIncident, CancellationToken.None);

        var list = await repository.ListAsync(
            "tenant-a",
            status: null,
            from: null,
            to: null,
            CancellationToken.None);

        list.Select(x => x.IncidentId)
            .Should()
            .Equal(newestIncident.IncidentId, middleIncident.IncidentId, oldestIncident.IncidentId);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistEvidence_AndDeleteIncidentShouldCascadeEvidence()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var incident = CreateIncident("tenant-a", DateTimeOffset.UtcNow.AddMinutes(-10));
        incident.AddEvidence("cam-1.jpg", "tenant-a/cam-1.jpg", DateTimeOffset.UtcNow.AddMinutes(-8));
        incident.AddEvidence("cam-2.jpg", "tenant-a/cam-2.jpg", DateTimeOffset.UtcNow.AddMinutes(-7));
        await repository.SaveAsync(incident, CancellationToken.None);

        var saved = await dbContext.Incidents
            .Include(x => x.EvidenceItems)
            .SingleAsync(x => x.Id == incident.IncidentId);

        saved.EvidenceItems.Should().HaveCount(2);

        dbContext.Incidents.Remove(saved);
        await dbContext.SaveChangesAsync();

        var evidenceCount = await dbContext.EvidenceItems.CountAsync();
        evidenceCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowConcurrencyException_WhenVersionSkipsCurrentRowVersion()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var incident = CreateIncident("tenant-a", DateTimeOffset.UtcNow.AddMinutes(-10));
        await repository.SaveAsync(incident, CancellationToken.None);

        var snapshot = incident.ToSnapshot();
        var staleUpdate = Incident.FromSnapshot(snapshot with
        {
            Title = "Stale update",
            Version = snapshot.Version + 2
        });

        Func<Task> act = () => repository.SaveAsync(staleUpdate, CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task GetAndList_ShouldEnforceTenantIsolation()
    {
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = new PostgreSqlIncidentRepository(dbContext);

        var tenantAIncident = CreateIncident("tenant-a", DateTimeOffset.UtcNow.AddMinutes(-20));
        var tenantBIncident = CreateIncident("tenant-b", DateTimeOffset.UtcNow.AddMinutes(-10));

        await repository.SaveAsync(tenantAIncident, CancellationToken.None);
        await repository.SaveAsync(tenantBIncident, CancellationToken.None);

        var wrongTenantLoad = await repository.GetAsync("tenant-a", tenantBIncident.IncidentId, CancellationToken.None);
        wrongTenantLoad.Should().BeNull();

        var tenantAList = await repository.ListAsync("tenant-a", null, null, null, CancellationToken.None);
        tenantAList.Should().ContainSingle();
        tenantAList[0].IncidentId.Should().Be(tenantAIncident.IncidentId);
    }

    private async Task<PublicSafetyDbContext> CreateCleanDbContextAsync()
    {
        var dbContext = PublicSafetyDbContext.CreateForPostgreSql(fixture.ConnectionString);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private static Incident CreateIncident(string tenantId, DateTimeOffset createdAt)
    {
        return Incident.Create(
            tenantId: tenantId,
            title: $"Incident for {tenantId}",
            description: "Synthetic integration test incident",
            priority: "High",
            location: "North District",
            reportedAt: createdAt,
            createdAt: createdAt);
    }
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("public_safety_lab_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
