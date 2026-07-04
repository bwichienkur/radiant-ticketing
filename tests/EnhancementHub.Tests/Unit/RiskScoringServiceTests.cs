using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Services;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class RiskScoringServiceTests
{
    private readonly RiskScoringService _sut = new();

    [Fact]
    public void CalculateRiskScore_WithMinimalAnalysis_ReturnsLowBaseScore()
    {
        var analysis = new AiAnalysisResult
        {
            Summary = "Minor UI tweak.",
            EstimatedEffortHours = 4
        };

        var score = _sut.CalculateRiskScore(analysis, repositoryContext: null);

        score.Should().Be(0.2);
        _sut.MapToRiskLevel(score).Should().Be(RiskLevel.Low);
    }

    [Fact]
    public void CalculateRiskScore_WithManyRisksAndEffort_IncreasesScore()
    {
        var analysis = new AiAnalysisResult
        {
            Summary = "Large cross-cutting change.",
            Risks = ["Data loss", "Downtime", "Regression", "Security"],
            ImpactedAreas = ["API", "Database", "Auth", "UI"],
            EstimatedEffortHours = 80
        };

        var repositoryContext = new RepositoryScanResult
        {
            DbContextTypes = ["MyApp.Data.AppDbContext"],
            Controllers = [new ScannedController { Name = "OrdersController" }]
        };

        var score = _sut.CalculateRiskScore(analysis, repositoryContext);

        score.Should().BeGreaterThanOrEqualTo(0.75);
        _sut.MapToRiskLevel(score).Should().Be(RiskLevel.Critical);
    }

    [Theory]
    [InlineData(0.10, RiskLevel.Low)]
    [InlineData(0.24, RiskLevel.Low)]
    [InlineData(0.25, RiskLevel.Medium)]
    [InlineData(0.49, RiskLevel.Medium)]
    [InlineData(0.50, RiskLevel.High)]
    [InlineData(0.74, RiskLevel.High)]
    [InlineData(0.75, RiskLevel.Critical)]
    [InlineData(1.00, RiskLevel.Critical)]
    public void MapToRiskLevel_UsesExpectedThresholds(double score, RiskLevel expected)
    {
        _sut.MapToRiskLevel(score).Should().Be(expected);
    }

    [Fact]
    public void CalculateRiskScore_ClampsScoreToOne()
    {
        var analysis = new AiAnalysisResult
        {
            Summary = "Extreme change.",
            Risks = Enumerable.Range(0, 20).Select(i => $"Risk {i}").ToList(),
            ImpactedAreas = Enumerable.Range(0, 20).Select(i => $"Area {i}").ToList(),
            EstimatedEffortHours = 200
        };

        var repositoryContext = new RepositoryScanResult
        {
            DbContextTypes = ["AppDbContext"],
            Controllers = [new ScannedController { Name = "ApiController" }]
        };

        _sut.CalculateRiskScore(analysis, repositoryContext).Should().Be(0.95);
    }
}
