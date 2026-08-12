using SvrMethod.Api.Application.Training;
using SvrMethod.Api.Domain;

namespace SvrMethod.Api.Application.Coach;

/// <summary>
/// Creates a safe, confirmation-required substitution proposal. It never
/// modifies a workout session or persists an action itself.
/// </summary>
public sealed class ExerciseSubstitutionTool(ExerciseAlternativesEngine alternativesEngine)
{
    public const string SafetyLevel = "Yellow";

    public ExerciseSubstitutionProposalDecision? CreateProposal(WorkoutSessionExercise current, Exercise replacement)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(replacement);

        if (current.Exercise is null || !alternativesEngine.IsApprovedAlternative(current.Exercise, replacement))
        {
            return null;
        }

        return new ExerciseSubstitutionProposalDecision(
            current.WorkoutSessionId,
            current.Id,
            replacement.Id,
            ExerciseAlternativesEngine.SamePrimaryMuscleGroupReasonCode,
            SafetyLevel,
            true);
    }
}

public sealed record ExerciseSubstitutionProposalDecision(
    Guid SessionId,
    Guid WorkoutSessionExerciseId,
    Guid ReplacementExerciseId,
    string ReasonCode,
    string SafetyLevel,
    bool RequiresConfirmation);
