using PersonalUltra.Domain;

namespace PersonalUltra.Application.Training;

/// <summary>
/// Applies the v0 exercise-substitution rule. An alternative must be a distinct,
/// valid exercise with the same primary muscle group. This engine only returns
/// read-only decisions; it never changes a session or creates a coach action.
/// </summary>
public sealed class ExerciseAlternativesEngine
{
    public const string SamePrimaryMuscleGroupReasonCode = "SAME_PRIMARY_MUSCLE_GROUP";

    public IReadOnlyList<ExerciseAlternativeDecision> FindApprovedAlternatives(Exercise original, IEnumerable<Exercise> catalog)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!HasValidDefinition(original)) return [];

        return catalog
            .Where(candidate => IsApprovedAlternative(original, candidate))
            .GroupBy(candidate => candidate.Id)
            .Select(group => group.First())
            .OrderBy(candidate => candidate.Name.Trim(), StringComparer.Ordinal)
            .Select(candidate => new ExerciseAlternativeDecision(candidate.Id, candidate.Name.Trim(), SamePrimaryMuscleGroupReasonCode))
            .ToList();
    }

    public bool IsApprovedAlternative(Exercise original, Exercise candidate) =>
        original is not null &&
        candidate is not null &&
        original.Id != candidate.Id &&
        HasValidDefinition(original) &&
        HasValidDefinition(candidate) &&
        string.Equals(NormalizeMuscleGroup(original.PrimaryMuscleGroup), NormalizeMuscleGroup(candidate.PrimaryMuscleGroup), StringComparison.OrdinalIgnoreCase);

    private static bool HasValidDefinition(Exercise exercise) =>
        exercise.Id != Guid.Empty &&
        !string.IsNullOrWhiteSpace(exercise.Name) &&
        !string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup);

    private static string NormalizeMuscleGroup(string muscleGroup) => muscleGroup.Trim();
}

public sealed record ExerciseAlternativeDecision(Guid ExerciseId, string Name, string ReasonCode);
