using PersonalUltra.Application.Safety;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class PainSafetyEngineTests
{
    [Theory]
    [InlineData(2, "Durante o exercício", "Green", "PAIN_LOW_INTENSITY", false)]
    [InlineData(4, "Durante o exercício", "Yellow", "PAIN_MODERATE_INTENSITY", true)]
    [InlineData(7, "Durante o exercício", "Red", "PAIN_HIGH_INTENSITY", true)]
    [InlineData(2, " ", "Red", "PAIN_CONTEXT_INCOMPLETE", true)]
    [InlineData(11, "Durante o exercício", "Red", "PAIN_INTENSITY_INVALID", true)]
    public void Applies_conservative_explainable_pain_rules(int intensity, string context, string level, string reasonCode, bool requiresConfirmation)
    {
        var decision = new PainSafetyEngine().Evaluate(intensity, context);

        Assert.Equal(level, decision.SafetyLevel);
        Assert.Equal(reasonCode, decision.ReasonCode);
        Assert.Equal(requiresConfirmation, decision.RequiresConfirmation);
        Assert.False(string.IsNullOrWhiteSpace(decision.Message));
    }
}
