using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Services;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase12Phase13CompletionTests
{
    [Fact]
    public void GitClone_InjectToken_AddsCredentialsToHttpsUrl()
    {
        var url = GitRepositoryCloneService.InjectToken(
            "https://github.com/org/repo.git",
            "secret-token");

        url.Should().Contain("secret-token");
        url.Should().StartWith("https://");
    }

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, ".pdf", true)]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, ".pdf", false)]
    public void AttachmentScan_StartsWithDetectsMagicBytes(byte[] buffer, string extension, bool expected)
    {
        AttachmentScanService.MagicSignatures.TryGetValue(extension, out var signatures).Should().BeTrue();
        AttachmentScanService.StartsWith(buffer, buffer.Length, signatures![0]).Should().Be(expected);
    }

    [Fact]
    public async Task BuildConnectionString_PostgreSql_IncludesHostAndDatabase()
    {
        var handler = new BuildDatabaseConnectionStringQueryHandler();
        var result = await handler.Handle(
            new BuildDatabaseConnectionStringQuery(
                DatabaseProviderType.PostgreSQL,
                "db.example.com",
                5432,
                "orders",
                "reader",
                "secret"),
            CancellationToken.None);

        result.ConnectionString.Should().Contain("Host=db.example.com");
        result.ConnectionString.Should().Contain("Database=orders");
    }
}
