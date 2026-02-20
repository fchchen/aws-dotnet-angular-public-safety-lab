using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PublicSafetyLab.Application.Common;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Incidents;

namespace PublicSafetyLab.Domain.Tests.Incidents;

public sealed class IncidentServiceTests
{
    [Fact]
    public async Task QueueIncidentProcessing_ShouldSetQueuedStatus_AndPublishMessage()
    {
        var incident = Incident.Create(
            tenantId: "demo",
            title: "Road closure",
            description: "Temporary closure",
            priority: "High",
            location: "Main",
            reportedAt: DateTimeOffset.UtcNow,
            createdAt: DateTimeOffset.UtcNow);

        var repository = new Mock<IIncidentRepository>();
        repository
            .Setup(x => x.GetAsync("demo", incident.IncidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        repository
            .Setup(x => x.SaveAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var evidenceStorage = new Mock<IEvidenceStorageService>();
        var queuePublisher = new Mock<IIncidentQueuePublisher>();

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var service = new IncidentService(
            repository.Object,
            evidenceStorage.Object,
            queuePublisher.Object,
            clock.Object,
            NullLogger<IncidentService>.Instance);

        await service.QueueIncidentProcessingAsync("demo", incident.IncidentId, "manual", CancellationToken.None);

        incident.Status.Should().Be(IncidentStatus.Queued);
        queuePublisher.Verify(
            x => x.PublishAsync(It.IsAny<PublicSafetyLab.Contracts.Queue.IncidentProcessingMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task QueueIncidentProcessing_WithExplicitCorrelationId_ShouldPublishSameCorrelationId()
    {
        var incident = Incident.Create(
            tenantId: "demo",
            title: "Road closure",
            description: "Temporary closure",
            priority: "High",
            location: "Main",
            reportedAt: DateTimeOffset.UtcNow,
            createdAt: DateTimeOffset.UtcNow);

        var repository = new Mock<IIncidentRepository>();
        repository
            .Setup(x => x.GetAsync("demo", incident.IncidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
        repository
            .Setup(x => x.SaveAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var evidenceStorage = new Mock<IEvidenceStorageService>();
        var queuePublisher = new Mock<IIncidentQueuePublisher>();
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var service = new IncidentService(
            repository.Object,
            evidenceStorage.Object,
            queuePublisher.Object,
            clock.Object,
            NullLogger<IncidentService>.Instance);

        const string correlationId = "corr-abc-123";

        await service.QueueIncidentProcessingAsync(
            "demo",
            incident.IncidentId,
            "manual",
            CancellationToken.None,
            correlationId);

        queuePublisher.Verify(
            x => x.PublishAsync(
                It.Is<PublicSafetyLab.Contracts.Queue.IncidentProcessingMessage>(m => m.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
