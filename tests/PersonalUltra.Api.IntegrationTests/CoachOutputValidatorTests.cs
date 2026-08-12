using System.Text.Json;
using PersonalUltra.Api.Application.Coach;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class CoachOutputValidatorTests
{
    [Theory]
    [InlineData(CoachMessageKinds.Text, false, false)]
    [InlineData(CoachMessageKinds.Choice, true, false)]
    [InlineData(CoachMessageKinds.ActionProposal, false, true)]
    [InlineData(CoachMessageKinds.ProgressInsight, false, false)]
    public void Validates_the_supported_structured_message_types(string kind, bool requiresUserInput, bool requiresConfirmation)
    {
        var result = new CoachOutputValidator().Validate(new CoachReply(kind, "Mensagem segura", "DEMO_REASON"));

        Assert.Equal(kind, result.Kind);
        Assert.Equal("Mensagem segura", result.Content);
        var metadata = JsonSerializer.Deserialize<CoachMessageMetadata>(result.MetadataJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(metadata);
        Assert.Equal(kind, metadata!.MessageType);
        Assert.Equal("DEMO_REASON", metadata.ReasonCode);
        Assert.Equal(requiresUserInput, metadata.RequiresUserInput);
        Assert.Equal(requiresConfirmation, metadata.RequiresConfirmation);
    }

    [Theory]
    [InlineData("Unknown", "Mensagem", "DEMO_REASON")]
    [InlineData(CoachMessageKinds.Text, "   ", "DEMO_REASON")]
    [InlineData(CoachMessageKinds.Text, "Mensagem", "invalid reason")]
    public void Rejects_unsupported_or_invalid_output(string kind, string content, string reasonCode)
    {
        Assert.Throws<CoachOutputValidationException>(() => new CoachOutputValidator().Validate(new CoachReply(kind, content, reasonCode)));
    }
}
