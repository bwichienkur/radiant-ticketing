using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Infrastructure.Services.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnhancementHub.Tests.Unit;

public sealed class S3FileStorageServiceTests
{
    [Fact]
    public void BuildKey_SanitizesContainerAndFileName()
    {
        var key = S3FileStorageService.BuildKey("requests-abc", "report (1).pdf");
        key.Should().StartWith("requests-abc/");
        key.Should().EndWith("_report (1).pdf");
        key.Should().NotContain("\\");
    }
}

public sealed class VectorEmbeddingNormalizerTests
{
    [Theory]
    [InlineData(4, new float[] { 1f, 2f }, new float[] { 1f, 2f, 1f, 2f })]
    [InlineData(4, new float[] { 1f, 2f, 3f, 4f, 5f }, new float[] { 1f, 2f, 3f, 4f })]
    public void Normalize_PadsOrTruncatesToConfiguredDimensions(int dimensions, float[] input, float[] expected) =>
        VectorEmbeddingNormalizer.Normalize(input, dimensions).Should().Equal(expected);
}

public sealed class CompositeNotificationPublisherTests
{
    [Fact]
    public async Task PublishAsync_FansOutToAllPublishers()
    {
        var first = new RecordingPublisher();
        var second = new RecordingPublisher();
        var composite = new CompositeNotificationPublisher(
            [first, second],
            NullLogger<CompositeNotificationPublisher>.Instance);

        await composite.PublishAsync("DriftDetected", "Title", "Message");

        first.Calls.Should().Be(1);
        second.Calls.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_ContinuesWhenOnePublisherFails()
    {
        var failing = new FailingPublisher();
        var succeeding = new RecordingPublisher();
        var composite = new CompositeNotificationPublisher(
            [failing, succeeding],
            NullLogger<CompositeNotificationPublisher>.Instance);

        await composite.PublishAsync("IndexComplete", "Title", "Message");

        succeeding.Calls.Should().Be(1);
    }

    private sealed class RecordingPublisher : INotificationPublisher
    {
        public int Calls { get; private set; }

        public Task PublishAsync(string eventType, string title, string message, object? data = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPublisher : INotificationPublisher
    {
        public Task PublishAsync(string eventType, string title, string message, object? data = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("boom");
    }
}
