using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

public sealed class DemoDataSeeder(PersonalUltraDbContext dbContext, TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (!await dbContext.Trainers.AnyAsync(x => x.Id == DemoIds.TrainerId, cancellationToken))
        {
            var trainer = new Trainer { Id = DemoIds.TrainerId, Name = "Alex Personal", CreatedAt = now };
            dbContext.Add(trainer);
            dbContext.Add(new TrainerBranding { Id = Guid.NewGuid(), Trainer = trainer, DisplayName = "Alex Personal", PrimaryColor = "#FF6B00" });
        }

        if (!await dbContext.Students.AnyAsync(x => x.Id == DemoIds.StudentId, cancellationToken))
            dbContext.Add(new Student { Id = DemoIds.StudentId, FirstName = "Rafa", LastName = "Silva", Email = "demo@student.personalultra.local", CreatedAt = now });

        if (!await dbContext.TrainerStudents.AnyAsync(x => x.TrainerId == DemoIds.TrainerId && x.StudentId == DemoIds.StudentId, cancellationToken))
            dbContext.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, StartedAt = now });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
