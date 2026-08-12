using PersonalUltra.Application.Coach;
using PersonalUltra.Application.Training;
using PersonalUltra.Domain;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseSubstitutionToolTests
{
    [Fact]
    public void Creates_a_confirmation_required_yellow_proposal_without_mutating_the_session()
    {
        var original = new Exercise { Id = Guid.NewGuid(), Name = "Supino reto", PrimaryMuscleGroup = "Peito" };
        var replacement = new Exercise { Id = Guid.NewGuid(), Name = "Supino com halteres", PrimaryMuscleGroup = "peito" };
        var current = new WorkoutSessionExercise { Id = Guid.NewGuid(), WorkoutSessionId = Guid.NewGuid(), ExerciseId = original.Id, Exercise = original };
        var tool = new ExerciseSubstitutionTool(new ExerciseAlternativesEngine());

        var proposal = tool.CreateProposal(current, replacement);

        Assert.NotNull(proposal);
        Assert.Equal(current.WorkoutSessionId, proposal!.SessionId);
        Assert.Equal(current.Id, proposal.WorkoutSessionExerciseId);
        Assert.Equal(replacement.Id, proposal.ReplacementExerciseId);
        Assert.Equal(ExerciseAlternativesEngine.SamePrimaryMuscleGroupReasonCode, proposal.ReasonCode);
        Assert.Equal("Yellow", proposal.SafetyLevel);
        Assert.True(proposal.RequiresConfirmation);
        Assert.Equal(original.Id, current.ExerciseId);
    }

    [Fact]
    public void Rejects_a_replacement_that_is_not_equivalent()
    {
        var original = new Exercise { Id = Guid.NewGuid(), Name = "Agachamento", PrimaryMuscleGroup = "Quadríceps" };
        var replacement = new Exercise { Id = Guid.NewGuid(), Name = "Puxada", PrimaryMuscleGroup = "Costas" };
        var current = new WorkoutSessionExercise { Id = Guid.NewGuid(), WorkoutSessionId = Guid.NewGuid(), ExerciseId = original.Id, Exercise = original };

        var proposal = new ExerciseSubstitutionTool(new ExerciseAlternativesEngine()).CreateProposal(current, replacement);

        Assert.Null(proposal);
        Assert.Equal(original.Id, current.ExerciseId);
    }
}
