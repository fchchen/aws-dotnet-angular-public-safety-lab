using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using PublicSafetyLab.Contracts.Queue;
using PublicSafetyLab.Infrastructure.Configuration;
using PublicSafetyLab.Infrastructure.Queue;
using System.Text.Json;

namespace PublicSafetyLab.Infrastructure.IntegrationTests.Queue;

public sealed class SqsIncidentQueueClientTests
{
    [Fact]
    public async Task ReceiveAsync_ShouldReturnEmpty_WhenSqsResponseIsNull()
    {
        var sqsMock = new Mock<IAmazonSQS>();
        sqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReceiveMessageResponse?)null);

        var options = Options.Create(new AwsResourceOptions
        {
            IncidentQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/demo"
        });

        var client = new SqsIncidentQueueClient(sqsMock.Object, options);
        var messages = await client.ReceiveAsync(5, CancellationToken.None);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ReceiveAsync_ShouldReturnEmpty_WhenSqsResponseContainsNoMessages()
    {
        var sqsMock = new Mock<IAmazonSQS>();
        sqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse
            {
                Messages = null
            });

        var options = Options.Create(new AwsResourceOptions
        {
            IncidentQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/demo"
        });

        var client = new SqsIncidentQueueClient(sqsMock.Object, options);
        var messages = await client.ReceiveAsync(5, CancellationToken.None);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ReceiveAsync_ShouldSkipInvalidMessages_AndReturnValidEnvelopes()
    {
        var incidentId = Guid.NewGuid();
        var valid = new IncidentProcessingMessage(
            MessageType: "IncidentProcessingRequested",
            TenantId: "demo",
            IncidentId: incidentId,
            CorrelationId: "corr-valid",
            OccurredAt: DateTimeOffset.UtcNow,
            Reason: "test");

        var sqsMock = new Mock<IAmazonSQS>();
        sqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse
            {
                Messages =
                [
                    new Message { Body = "{invalid-json", ReceiptHandle = "bad-1" },
                    new Message { Body = JsonSerializer.Serialize(valid), ReceiptHandle = "good-1" },
                    new Message { Body = JsonSerializer.Serialize(valid), ReceiptHandle = "" }
                ]
            });

        var options = Options.Create(new AwsResourceOptions
        {
            IncidentQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/demo"
        });

        var client = new SqsIncidentQueueClient(sqsMock.Object, options);
        var messages = await client.ReceiveAsync(5, CancellationToken.None);

        messages.Should().HaveCount(1);
        messages[0].ReceiptHandle.Should().Be("good-1");
        messages[0].Message.IncidentId.Should().Be(incidentId);
    }

    [Fact]
    public async Task PublishAsync_ShouldSendSerializedMessageToConfiguredQueue()
    {
        var sqsMock = new Mock<IAmazonSQS>();
        sqsMock
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        var options = Options.Create(new AwsResourceOptions
        {
            IncidentQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/demo"
        });

        var client = new SqsIncidentQueueClient(sqsMock.Object, options);

        await client.PublishAsync(
            new IncidentProcessingMessage(
                MessageType: "IncidentProcessingRequested",
                TenantId: "demo",
                IncidentId: Guid.NewGuid(),
                CorrelationId: "corr-1",
                OccurredAt: DateTimeOffset.UtcNow,
                Reason: "test"),
            CancellationToken.None);

        sqsMock.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(req =>
                req.QueueUrl == "https://sqs.us-east-1.amazonaws.com/123/demo" &&
                req.MessageBody.Contains("IncidentProcessingRequested")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
