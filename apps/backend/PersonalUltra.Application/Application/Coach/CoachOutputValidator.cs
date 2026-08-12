using System.Text.Json;
using System.Text.RegularExpressions;

namespace PersonalUltra.Application.Coach;

public static class CoachMessageKinds
{
    public const string Text = "Text";
    public const string Choice = "Choice";
    public const string ActionProposal = "ActionProposal";
    public const string ProgressInsight = "ProgressInsight";
}

// Stored in CoachMessage.MetadataJson. It describes presentation requirements,
// never an executable command or a database mutation.
public sealed record CoachMessageMetadata(
    string ReasonCode,
    string MessageType,
    bool RequiresUserInput,
    bool RequiresConfirmation);

public sealed record ValidatedCoachReply(string Kind, string Content, string MetadataJson);

public sealed class CoachOutputValidator
{
    private const int MaxContentLength = 2000;
    private static readonly Regex ReasonCodePattern = new("^[A-Z0-9_]{1,100}$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ValidatedCoachReply Validate(CoachReply reply)
    {
        if (reply is null || string.IsNullOrWhiteSpace(reply.Content))
            throw new CoachOutputValidationException("Coach output must include content.");

        var content = reply.Content.Trim();
        if (content.Length > MaxContentLength)
            throw new CoachOutputValidationException("Coach output exceeds the supported length.");
        var reasonCode = reply.ReasonCode ?? throw new CoachOutputValidationException("Coach output has an invalid reason code.");
        if (!ReasonCodePattern.IsMatch(reasonCode))
            throw new CoachOutputValidationException("Coach output has an invalid reason code.");

        var metadata = reply.Kind switch
        {
            CoachMessageKinds.Text => new CoachMessageMetadata(reasonCode, CoachMessageKinds.Text, false, false),
            CoachMessageKinds.Choice => new CoachMessageMetadata(reasonCode, CoachMessageKinds.Choice, true, false),
            // A proposal is presentation-only here. M1-024 remains responsible
            // for confirmation and applying any future material action.
            CoachMessageKinds.ActionProposal => new CoachMessageMetadata(reasonCode, CoachMessageKinds.ActionProposal, false, true),
            CoachMessageKinds.ProgressInsight => new CoachMessageMetadata(reasonCode, CoachMessageKinds.ProgressInsight, false, false),
            _ => throw new CoachOutputValidationException("Coach output has an unsupported message type."),
        };

        return new ValidatedCoachReply(reply.Kind, content, JsonSerializer.Serialize(metadata, JsonOptions));
    }
}

public sealed class CoachOutputValidationException(string message) : Exception(message);
