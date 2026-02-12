using FluentAssertions;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Exceptions;
using PublicSafetyLab.Domain.Incidents;

namespace PublicSafetyLab.Domain.Tests.Incidents;

public sealed class IncidentTests
{
    [Fact]
    public void Create_ShouldInitializeIncident_WhenRequestIsValid()
    {
        var now = DateTimeOffset.UtcNow;

        var incident = Incident.Create(
            tenantId: "demo",
            title: "Road blockage",
            description: "Two vehicles blocking lane",
            priority: "High",
            location: "I-94 Exit 45",
            reportedAt: now,
            createdAt: now);

        incident.Status.Should().Be(IncidentStatus.New);
        incident.Evidence.Should().BeEmpty();
        incident.Version.Should().Be(1);
    }

    [Fact]
    public void Create_ShouldThrow_WhenTitleIsMissing()
    {
        var action = () => Incident.Create(
            tenantId: "demo",
            title: "   ",
            description: "desc",
            priority: "Medium",
            location: "Downtown",
            reportedAt: DateTimeOffset.UtcNow,
            createdAt: DateTimeOffset.UtcNow);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void MarkProcessed_ShouldThrow_WhenIncidentWasNotQueued()
    {
        var incident = Incident.Create(
            tenantId: "demo",
            title: "Power outage",
            description: "Grid outage",
            priority: "Critical",
            location: "Main St",
            reportedAt: DateTimeOffset.UtcNow,
            createdAt: DateTimeOffset.UtcNow);

        var action = () => incident.MarkProcessed(DateTimeOffset.UtcNow);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void MarkQueued_ThenMarkProcessed_ShouldUpdateStatus()
    {
        var incident = Incident.Create(
            tenantId: "demo",
            title: "Fire alarm",
            description: "False positive",
            priority: "Low",
            location: "School",
            reportedAt: DateTimeOffset.UtcNow,
            createdAt: DateTimeOffset.UtcNow);

        incident.AddEvidence("photo.jpg", "tenant/demo/incident/1/photo.jpg", DateTimeOffset.UtcNow);
        incident.MarkQueued(DateTimeOffset.UtcNow);
        incident.MarkProcessed(DateTimeOffset.UtcNow);

        incident.Status.Should().Be(IncidentStatus.Processed);
        incident.ProcessedAt.Should().NotBeNull();
    }
}
