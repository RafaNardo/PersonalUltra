using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

public sealed class DemoDataSeeder(PersonalUltraDbContext dbContext, TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var trainer = await dbContext.Trainers.Include(x => x.Branding).SingleOrDefaultAsync(x => x.Id == DemoIds.TrainerId, cancellationToken);
        if (trainer is null)
        {
            trainer = new Trainer { Id = DemoIds.TrainerId, Name = "Severo", CreatedAt = now };
            dbContext.Add(trainer);
            dbContext.Add(new TrainerBranding { Id = Guid.NewGuid(), Trainer = trainer, DisplayName = "Severo", PrimaryColor = "#FF6B00" });
        }
        else
        {
            trainer.Name = "Severo";
            if (trainer.Branding is not null) trainer.Branding.DisplayName = "Severo";
        }

        var student = await dbContext.Students.SingleOrDefaultAsync(x => x.Id == DemoIds.StudentId, cancellationToken);
        if (student is null)
        {
            student = new Student { Id = DemoIds.StudentId, FirstName = "Rafa", LastName = "Silva", Email = "demo@student.personalultra.local", CreatedAt = now };
            dbContext.Add(student);
        }

        if (!await dbContext.TrainerStudents.AnyAsync(x => x.TrainerId == DemoIds.TrainerId && x.StudentId == DemoIds.StudentId, cancellationToken))
            dbContext.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, StartedAt = now });

        if (!await dbContext.StudentWorkouts.AnyAsync(x => x.StudentId == DemoIds.StudentId, cancellationToken))
        {
            var workout = new StudentWorkout { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Força · Treino A", Notes = "Foco em execução consistente e progressão gradual.", RecommendedDay = 1, IsRecommended = true, CreatedAt = now };
            workout.Exercises.AddRange(new[] { ("Agachamento livre", 4, 8, 90), ("Supino reto", 4, 10, 75), ("Remada baixa", 3, 10, 75) }.Select((x, i) => new StudentWorkoutExercise { Id = Guid.NewGuid(), StudentWorkoutId = workout.Id, Name = x.Item1, Sequence = i + 1, Sets = x.Item2, Repetitions = x.Item3, RestSeconds = x.Item4 }));
            dbContext.Add(workout);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
