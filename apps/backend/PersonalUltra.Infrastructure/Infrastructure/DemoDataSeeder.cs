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

        if (!await dbContext.Students.AnyAsync(x => x.Id == DemoIds.StudentId, cancellationToken))
            dbContext.Add(new Student { Id = DemoIds.StudentId, FirstName = "Rafa", LastName = "Silva", Email = "demo@student.personalultra.local", CreatedAt = now });

        if (!await dbContext.TrainerStudents.AnyAsync(x => x.TrainerId == DemoIds.TrainerId && x.StudentId == DemoIds.StudentId, cancellationToken))
            dbContext.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, StartedAt = now });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
