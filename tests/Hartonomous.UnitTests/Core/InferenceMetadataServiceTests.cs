using Hartonomous.Core.Services;

namespace Hartonomous.UnitTests.Core;

public sealed class InferenceMetadataServiceTests
{
    private readonly InferenceMetadataService _service = new();

    [Theory]
    [InlineData(true, "anything", "chain-of-thought")]
    [InlineData(true, "", "chain-of-thought")]
    public void DetermineReasoningMode_ReturnsChainOfThought_WhenMultiStep(bool requiresMultiStep, string task, string expected)
    {
        var mode = _service.DetermineReasoningMode(task, requiresMultiStep);

        Assert.Equal(expected, mode);
    }

    [Theory]
    [InlineData("Analyze quarterly metrics for anomalies.")]
    [InlineData("Reason through compliance obligations.")]
    [InlineData("Explain the decision tree steps.")]
    [InlineData("Compare product roadmaps.")]
    public void DetermineReasoningMode_ReturnsAnalytical_ForAnalysisKeywords(string task)
    {
        var mode = _service.DetermineReasoningMode(task, requiresMultiStep: false);

        Assert.Equal("analytical", mode);
    }

    [Theory]
    [InlineData("Create a marketing tagline for the fall campaign.")]
    [InlineData("Generate a bedtime story about space explorers.")]
    [InlineData("Design a new user onboarding flow.")]
    [InlineData("Write a poem in the style of Neruda.")]
    public void DetermineReasoningMode_ReturnsCreative_ForCreativeKeywords(string task)
    {
        var mode = _service.DetermineReasoningMode(task, requiresMultiStep: false);

        Assert.Equal("creative", mode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Summarize the meeting notes.")]
    public void DetermineReasoningMode_DefaultsToDirect_WhenNoMatch(string? task)
    {
        var mode = _service.DetermineReasoningMode(task ?? string.Empty, requiresMultiStep: false);

        Assert.Equal("direct", mode);
    }

    [Theory]
    [InlineData(500, false, false, 1)]
    [InlineData(1500, false, false, 2)]
    [InlineData(2500, false, false, 3)]
    [InlineData(4500, false, false, 4)]
    [InlineData(9000, false, false, 5)]
    public void CalculateComplexity_UsesTokenThresholds(int tokens, bool multiModal, bool toolUse, int expected)
    {
        var complexity = _service.CalculateComplexity(tokens, multiModal, toolUse);

        Assert.Equal(expected, complexity);
    }

    [Fact]
    public void CalculateComplexity_AddsModifiers_ForModalitiesAndTools()
    {
        var complexity = _service.CalculateComplexity(1500, requiresMultiModal: true, requiresToolUse: true);

        Assert.Equal(6, complexity);
    }

    [Fact]
    public void CalculateComplexity_TopsOutAtNineWithCurrentRules()
    {
        var complexity = _service.CalculateComplexity(20000, requiresMultiModal: true, requiresToolUse: true);

        Assert.Equal(9, complexity);
    }

    [Theory]
    [InlineData("critical", 9, "realtime")]
    [InlineData("HIGH", 2, "realtime")]
    [InlineData("medium", 3, "expedited")]
    [InlineData(null, 4, "expedited")]
    [InlineData("low", 9, "standard")]
    public void DetermineSla_ReturnsExpectedTier(string? priority, int complexity, string expected)
    {
        var sla = priority is null
            ? _service.DetermineSla(null!, complexity)
            : _service.DetermineSla(priority, complexity);

        Assert.Equal(expected, sla);
    }

    [Theory]
    [InlineData(null, 3, 15)]
    [InlineData("gpt-3.5-turbo", 4, 8)]
    [InlineData("Azure GPT-4", 3, 15)]
    [InlineData("DALL-E-3", 2, 20)]
    [InlineData("custom-model", 6, 30)]
    public void EstimateResponseTime_UsesModelFamilies(string? modelName, int complexity, int expectedSeconds)
    {
        var estimate = _service.EstimateResponseTime(modelName ?? string.Empty, complexity);

        Assert.Equal(expectedSeconds, estimate);
    }
}
