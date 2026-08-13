using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

public sealed class DemoDataSeeder(PersonalUltraDbContext dbContext, TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await SeedExercisesAsync(cancellationToken);
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

        await SeedDemoWorkoutsAsync(now, cancellationToken);

        if (!await dbContext.NutritionPlans.AnyAsync(x => x.StudentId == DemoIds.StudentId, cancellationToken))
        {
            var plan = new NutritionPlan { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Base de performance", Notes = "Plano demonstrativo cadastrado pelo personal.", UpdatedAt = now };
            foreach (var (name, foods) in new[] { ("Café da manhã", new[] { ("Ovos", 150m), ("Fruta", 120m) }), ("Almoço", new[] { ("Arroz", 150m), ("Frango", 180m), ("Salada", 100m) }), ("Jantar", new[] { ("Batata", 180m), ("Carne magra", 160m) }) }.Select((x, i) => (x.Item1, x.Item2)))
            {
                var meal = new Meal { Id = Guid.NewGuid(), NutritionPlanId = plan.Id, Name = name, Sequence = plan.Meals.Count + 1 };
                meal.Foods.AddRange(foods.Select(f => new MealFood { Id = Guid.NewGuid(), MealId = meal.Id, FoodName = f.Item1, QuantityGrams = f.Item2 })); plan.Meals.Add(meal);
            }
            dbContext.Add(plan);
        }
        if (!await dbContext.WeightEntries.AnyAsync(x => x.StudentId == DemoIds.StudentId, cancellationToken))
            dbContext.WeightEntries.AddRange(new WeightEntry { Id = Guid.NewGuid(), StudentId = DemoIds.StudentId, WeightKg = 78.4m, RecordedAt = now.AddDays(-21) }, new WeightEntry { Id = Guid.NewGuid(), StudentId = DemoIds.StudentId, WeightKg = 77.8m, RecordedAt = now.AddDays(-7) }, new WeightEntry { Id = Guid.NewGuid(), StudentId = DemoIds.StudentId, WeightKg = 77.5m, RecordedAt = now });

        var demoNames = new[] { ("Bruna", "Costa"), ("Caio", "Mendes"), ("Duda", "Alves"), ("Enzo", "Lima"), ("Fabi", "Rocha"), ("Gabi", "Nunes"), ("Hugo", "Dias"), ("Iara", "Moraes"), ("João", "Pires"), ("Karla", "Reis"), ("Leo", "Santos"), ("Mia", "Freitas") };
        foreach (var (first, last) in demoNames)
        {
            var email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@demo.personalultra.local";
            var extra = await dbContext.Students.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
            if (extra is null) { extra = new Student { Id = Guid.NewGuid(), FirstName = first, LastName = last, Email = email, CreatedAt = now }; dbContext.Add(extra); }
            if (!await dbContext.TrainerStudents.AnyAsync(x => x.TrainerId == DemoIds.TrainerId && x.StudentId == extra.Id, cancellationToken)) dbContext.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = extra.Id, StartedAt = now });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedExercisesAsync(CancellationToken cancellationToken)
    {
        var existingSlugs = await dbContext.Exercises
            .AsNoTracking()
            .Select(x => x.Slug)
            .ToHashSetAsync(cancellationToken);

        foreach (var seed in ExerciseCatalogSeed.Exercises)
        {
            // Slug is the stable seed key. Existing catalog rows are left
            // untouched so a demo reset cannot overwrite real data.
            if (!existingSlugs.Contains(seed.Slug))
                dbContext.Exercises.Add(seed.ToEntity());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoWorkoutsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var catalog = await dbContext.Exercises
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Slug, cancellationToken);
        var seededIds = DemoWorkoutSeed.Workouts.Select(x => DemoWorkoutSeed.IdFor($"workout:{x.Key}")).ToHashSet();
        var existing = await dbContext.StudentWorkouts
            .Include(x => x.Exercises)
            .Where(x => x.TrainerId == DemoIds.TrainerId && x.StudentId == DemoIds.StudentId)
            .ToListAsync(cancellationToken);

        // The original M3 seed used this one workout. It is known demo data,
        // so only its recommendation flag is migrated; sessions and exercise
        // edits remain untouched. User-created workouts are never changed.
        foreach (var legacy in existing.Where(x => !seededIds.Contains(x.Id) && IsLegacyWorkout(x)))
            legacy.IsRecommended = false;

        // Never create a second recommendation while upgrading a database
        // that already contains an unknown (possibly user-edited) one. This
        // flag only controls newly added seeded rows; existing rows are kept.
        var recommendationAlreadyExists = existing.Any(x => x.IsRecommended);

        foreach (var seed in DemoWorkoutSeed.Workouts)
        {
            var workoutId = DemoWorkoutSeed.IdFor($"workout:{seed.Key}");
            if (existing.Any(x => x.Id == workoutId))
                continue;

            // A partially provisioned local database may not have the full
            // catalog yet. Leave that workout for the next seed run instead
            // of creating a partial prescription or overwriting user data.
            if (seed.Exercises.Any(x => !catalog.ContainsKey(x.Slug)))
                continue;

            var workout = new StudentWorkout
            {
                Id = workoutId,
                TrainerId = DemoIds.TrainerId,
                StudentId = DemoIds.StudentId,
                Name = seed.Name,
                Notes = seed.Notes,
                RecommendedDay = seed.RecommendedDay,
                IsRecommended = seed.IsRecommended && !recommendationAlreadyExists,
                CreatedAt = now,
            };

            foreach (var (exercise, sequence) in seed.Exercises.Select((x, i) => (x, i + 1)))
            {
                var catalogExercise = catalog[exercise.Slug];

                var snapshot = StudentWorkoutExercise.FromCatalog(workout.Id, catalogExercise, sequence, exercise.Sets, exercise.RepetitionsMin, exercise.RepetitionsMax, exercise.RestSeconds, exercise.Notes);
                snapshot.Id = DemoWorkoutSeed.IdFor($"workout:{seed.Key}:exercise:{exercise.Slug}");
                workout.Exercises.Add(snapshot);
            }

            dbContext.StudentWorkouts.Add(workout);
            recommendationAlreadyExists |= workout.IsRecommended;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsLegacyWorkout(StudentWorkout workout) =>
        workout.Name == "Força · Treino A" &&
        workout.Exercises.Count == 3 &&
        workout.Exercises.OrderBy(x => x.Sequence).Select(x => (x.Name, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds)).SequenceEqual(
        [
            ("Agachamento livre", 1, 4, 8, 8, 90),
            ("Supino reto com barra", 2, 4, 10, 10, 75),
            ("Remada baixa", 3, 3, 10, 10, 75),
        ]);
}
