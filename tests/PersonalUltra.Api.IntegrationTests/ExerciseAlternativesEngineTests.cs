using PersonalUltra.Api.Application.Training;
using PersonalUltra.Api.Domain;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseAlternativesEngineTests
{
    [Fact]
    public void Returns_only_distinct_valid_exercises_with_the_same_primary_muscle_group()
    {
        var original = Exercise("Remada baixa", "Costas", "00000000-0000-0000-0000-000000000001");
        var validB = Exercise("Puxada frontal", " costas ", "00000000-0000-0000-0000-000000000003");
        var validA = Exercise("Remada unilateral", "COSTAS", "00000000-0000-0000-0000-000000000002");
        var differentMuscle = Exercise("Supino reto", "Peito", "00000000-0000-0000-0000-000000000004");
        var invalidName = Exercise(" ", "Costas", "00000000-0000-0000-0000-000000000005");
        var engine = new ExerciseAlternativesEngine();

        var alternatives = engine.FindApprovedAlternatives(original, [original, validB, validA, validA, differentMuscle, invalidName]);

        Assert.Equal([validB.Id, validA.Id], alternatives.Select(item => item.ExerciseId));
        Assert.All(alternatives, item => Assert.Equal(ExerciseAlternativesEngine.SamePrimaryMuscleGroupReasonCode, item.ReasonCode));
        Assert.Equal(["Puxada frontal", "Remada unilateral"], alternatives.Select(item => item.Name));
    }

    [Fact]
    public void Rejects_self_replacements_and_incomplete_exercise_definitions()
    {
        var original = Exercise("Agachamento livre", "Quadríceps", "00000000-0000-0000-0000-000000000010");
        var invalidGroup = Exercise("Leg press", " ", "00000000-0000-0000-0000-000000000011");
        var emptyId = Exercise("Afundo", "Quadríceps", Guid.Empty.ToString());
        var engine = new ExerciseAlternativesEngine();

        Assert.False(engine.IsApprovedAlternative(original, original));
        Assert.False(engine.IsApprovedAlternative(original, invalidGroup));
        Assert.False(engine.IsApprovedAlternative(original, emptyId));
        Assert.Empty(engine.FindApprovedAlternatives(emptyId, [original]));
    }

    private static Exercise Exercise(string name, string primaryMuscleGroup, string id) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        PrimaryMuscleGroup = primaryMuscleGroup,
    };
}
