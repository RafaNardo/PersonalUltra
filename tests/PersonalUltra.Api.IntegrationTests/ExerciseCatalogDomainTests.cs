using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseCatalogDomainTests
{
    [Fact]
    public async Task Demo_seed_combines_the_legacy_catalog_with_generated_remote_references()
    {
        await using var db = CreateDatabase();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var exercises = await db.Exercises.AsNoTracking().ToListAsync();

        Assert.Equal(231, exercises.Count);
        Assert.All(exercises, exercise =>
        {
            Assert.False(string.IsNullOrWhiteSpace(exercise.Name));
            Assert.False(string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup));
            Assert.EndsWith(".webp", exercise.ImageRef);
            Assert.True(exercise.IsActive);
        });
        Assert.Equal(231, exercises.Count(exercise => exercise.ImageRef.StartsWith("media://exercise-catalog/delivery/v1/", StringComparison.Ordinal)));
        Assert.Contains(exercises, exercise => exercise.Name == "Supino reto com barra" && exercise.PrimaryMuscleGroup == "Peito");
        Assert.Contains(exercises, exercise => exercise.Name == "Remada baixa" && exercise.PrimaryMuscleGroup == "Costas");
        Assert.Contains(exercises, exercise => exercise.Name == "Desenvolvimento com halteres" && exercise.PrimaryMuscleGroup == "Ombros");
        Assert.Contains(exercises, exercise => exercise.Name == "Rosca direta com barra" && exercise.PrimaryMuscleGroup == "Bíceps");
        Assert.Contains(exercises, exercise => exercise.Name == "Agachamento livre" && exercise.PrimaryMuscleGroup == "Quadríceps");
        Assert.Contains(exercises, exercise => exercise.Name == "Elevação pélvica com barra" && exercise.PrimaryMuscleGroup == "Glúteos");
        Assert.Equal(
            ["Bíceps", "Cardio", "Core", "Corpo inteiro", "Costas", "Glúteos", "Ombros", "Panturrilhas", "Peito", "Posteriores da coxa", "Quadríceps", "Tríceps"],
            exercises.Select(exercise => exercise.PrimaryMuscleGroup).Distinct().OrderBy(group => group).ToArray());
    }

    [Fact]
    public async Task Demo_seed_is_idempotent_for_catalog_rows()
    {
        await using var db = CreateDatabase();
        var seeder = new DemoDataSeeder(db, TimeProvider.System);

        await seeder.SeedAsync(CancellationToken.None);
        var first = await db.Exercises.AsNoTracking().OrderBy(x => x.Slug).Select(x => new { x.Slug, x.Id, x.ImageRef }).ToListAsync();

        await seeder.SeedAsync(CancellationToken.None);
        var second = await db.Exercises.AsNoTracking().OrderBy(x => x.Slug).Select(x => new { x.Slug, x.Id, x.ImageRef }).ToListAsync();

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Demo_seed_does_not_overwrite_an_existing_catalog_row()
    {
        await using var db = CreateDatabase();
        var existing = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Nome curado pelo sistema",
            Slug = "supino-reto-com-barra",
            PrimaryMuscleGroup = "Peito customizado",
            ImageRef = "assets/custom/supino.png",
            IsActive = false,
        };
        db.Exercises.Add(existing);
        await db.SaveChangesAsync();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var persisted = await db.Exercises.AsNoTracking().SingleAsync(x => x.Slug == existing.Slug);
        Assert.Equal(existing.Id, persisted.Id);
        Assert.Equal(existing.Name, persisted.Name);
        Assert.Equal(existing.PrimaryMuscleGroup, persisted.PrimaryMuscleGroup);
        Assert.Equal(existing.ImageRef, persisted.ImageRef);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task Demo_seed_migrates_only_a_system_owned_donor_reference_to_v3()
    {
        await using var db = CreateDatabase();
        db.Exercises.Add(new Exercise
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Supino reto com barra",
            Slug = "supino-reto-com-barra",
            PrimaryMuscleGroup = "Peito",
            ImageRef = "assets/training/supino-reto-com-barra.png",
        });
        await db.SaveChangesAsync();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var exercise = await db.Exercises.AsNoTracking().SingleAsync(x => x.Slug == "supino-reto-com-barra");
        Assert.Equal("media://exercise-catalog/delivery/v1/supino-reto-com-barra.webp", exercise.ImageRef);
    }

    [Fact]
    public void Exercise_is_a_global_catalog_entity_with_a_unique_stable_key()
    {
        var options = new DbContextOptionsBuilder<PersonalUltraDbContext>()
            .UseInMemoryDatabase($"exercise-catalog-model-{Guid.NewGuid():N}")
            .Options;
        using var db = new PersonalUltraDbContext(options);

        var entity = db.Model.FindEntityType(typeof(Exercise));

        Assert.NotNull(entity);
        Assert.Equal("exercises", entity!.GetTableName());
        Assert.Equal("training", entity.GetSchema());
        Assert.Null(entity.FindProperty("TrainerId"));
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(Exercise.Slug)]));
    }

    [Fact]
    public void Exercise_contains_the_required_catalog_display_metadata()
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Supino reto com barra",
            Slug = "supino-reto-com-barra",
            PrimaryMuscleGroup = "Peito",
            Equipment = "Barra",
            ImageRef = "assets/training/supino-reto-com-barra.png",
            Instructions = "Mantenha as escápulas retraídas."
        };

        Assert.True(exercise.IsActive);
        Assert.Equal("Peito", exercise.PrimaryMuscleGroup);
        Assert.Equal("assets/training/supino-reto-com-barra.png", exercise.ImageRef);
    }

    [Fact]
    public void Student_workout_persists_a_unique_suggested_order_for_each_active_student_routine()
    {
        using var db = CreateDatabase();

        var entity = db.Model.FindEntityType(typeof(StudentWorkout));

        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindProperty(nameof(StudentWorkout.SuggestedOrder)));
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(StudentWorkout.StudentId), nameof(StudentWorkout.SuggestedOrder)]));
        Assert.True(index.IsUnique);
        Assert.Equal("\"IsActive\"", index.GetFilter());
    }

    [Fact]
    public async Task Demo_seed_retires_all_donor_asset_references_in_favor_of_v3()
    {
        await using var db = CreateDatabase();
        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var legacyIds = Enumerable.Range(1, 28)
            .Select(index => Guid.Parse($"10000000-0000-0000-0000-{index:000000000000}"))
            .ToHashSet();
        var actual = (await db.Exercises.AsNoTracking().ToListAsync())
            .Where(exercise => legacyIds.Contains(exercise.Id))
            .Select(exercise => exercise.ImageRef).OrderBy(x => x).ToArray();

        Assert.Equal(28, actual.Length);
        Assert.Contains("media://exercise-catalog/delivery/v1/agachamento-livre.webp", actual);
        Assert.Contains("media://exercise-catalog/delivery/v1/supino-reto-com-barra.webp", actual);
        Assert.DoesNotContain(await db.Exercises.Select(x => x.ImageRef).ToArrayAsync(),
            imageRef => imageRef.StartsWith("assets/training/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Demo_seed_creates_four_complete_named_workouts_with_catalog_snapshots()
    {
        await using var db = CreateDatabase();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var workouts = await db.StudentWorkouts
            .AsNoTracking()
            .Include(x => x.Exercises)
            .Where(x => x.StudentId == DemoIds.StudentId)
            .OrderBy(x => x.SuggestedOrder)
            .ToListAsync();

        Assert.Equal(["Upper A", "Lower A", "Upper B", "Lower B"], workouts.Select(x => x.Name));
        Assert.Equal([1, 2, 3, 4], workouts.Select(x => x.SuggestedOrder));
        Assert.All(workouts, workout =>
        {
            Assert.Equal(6, workout.Exercises.Count);
            Assert.Equal([1, 2, 3, 4, 5, 6], workout.Exercises.OrderBy(x => x.Sequence).Select(x => x.Sequence));
            Assert.All(workout.Exercises, exercise =>
            {
                Assert.NotNull(exercise.ExerciseId);
                Assert.StartsWith("media://exercise-catalog/", exercise.ImageRef);
                Assert.DoesNotContain("mock", exercise.ImageRef!, StringComparison.OrdinalIgnoreCase);
                Assert.False(string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup));
                Assert.True(exercise.RepetitionsMin <= exercise.RepetitionsMax);
                Assert.InRange(exercise.Sets, 1, 20);
            });
        });
    }

    [Fact]
    public async Task Demo_workout_seed_is_idempotent_and_uses_stable_ids()
    {
        await using var db = CreateDatabase();
        var seeder = new DemoDataSeeder(db, TimeProvider.System);

        await seeder.SeedAsync(CancellationToken.None);
        var first = await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == DemoIds.StudentId).Include(x => x.Exercises).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.SuggestedOrder, Exercises = x.Exercises.OrderBy(e => e.Sequence).Select(e => new { e.Id, e.ExerciseId, e.Sets, e.RepetitionsMin, e.RepetitionsMax, e.RestSeconds }).ToList() }).ToListAsync();

        await seeder.SeedAsync(CancellationToken.None);
        var second = await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == DemoIds.StudentId).Include(x => x.Exercises).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.SuggestedOrder, Exercises = x.Exercises.OrderBy(e => e.Sequence).Select(e => new { e.Id, e.ExerciseId, e.Sets, e.RepetitionsMin, e.RepetitionsMax, e.RestSeconds }).ToList() }).ToListAsync();

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Id, second[i].Id);
            Assert.Equal(first[i].Name, second[i].Name);
            Assert.Equal(first[i].SuggestedOrder, second[i].SuggestedOrder);
            Assert.Equal(first[i].Exercises.Count, second[i].Exercises.Count);
            for (var j = 0; j < first[i].Exercises.Count; j++)
                Assert.Equal(first[i].Exercises[j], second[i].Exercises[j]);
        }
    }

    [Fact]
    public async Task Demo_workout_upgrade_does_not_replace_a_user_workout_or_its_history()
    {
        await using var db = CreateDatabase();
        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);
        var customId = Guid.NewGuid();
        var customExercise = await db.Exercises.AsNoTracking().FirstAsync();
        var custom = new StudentWorkout { Id = customId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Treino personalizado", Notes = "Edição do aluno", SuggestedOrder = 5, CreatedAt = DateTimeOffset.UtcNow };
        custom.Exercises.Add(StudentWorkoutExercise.FromCatalog(customId, customExercise, 1, 2, 15, 20, 45, "Minha nota"));
        db.StudentWorkouts.Add(custom);
        await db.SaveChangesAsync();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var persisted = await db.StudentWorkouts.AsNoTracking().Include(x => x.Exercises).SingleAsync(x => x.Id == customId);
        Assert.Equal("Treino personalizado", persisted.Name);
        Assert.Equal("Edição do aluno", persisted.Notes);
        Assert.Equal("Minha nota", Assert.Single(persisted.Exercises).Notes);
        Assert.Equal(5, await db.StudentWorkouts.CountAsync(x => x.StudentId == DemoIds.StudentId));
    }

    [Fact]
    public async Task Demo_workout_upgrade_migrates_only_the_known_legacy_seed_flag_and_keeps_history()
    {
        await using var db = CreateDatabase();
        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);
        var catalog = await db.Exercises.AsNoTracking().ToDictionaryAsync(x => x.Slug);
        var legacyId = Guid.NewGuid();
        var legacy = new StudentWorkout { Id = legacyId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Força · Treino A", Notes = "Foco em execução consistente e progressão gradual.", SuggestedOrder = 5, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["agachamento-livre"], 1, 4, 8, 8, 90));
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["supino-reto-com-barra"], 2, 4, 10, 10, 75));
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["remada-baixa"], 3, 3, 10, 10, 75));
        var session = new WorkoutSession { Id = Guid.NewGuid(), StudentId = DemoIds.StudentId, StudentWorkoutId = legacyId, StartedAt = DateTimeOffset.UtcNow.AddHours(-2), Status = "InProgress" };
        session.Exercises.AddRange(legacy.Exercises.Select(x => WorkoutSessionExercise.FromStudentWorkout(session.Id, x)));
        db.StudentWorkouts.Add(legacy);
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var migrated = await db.StudentWorkouts.AsNoTracking().SingleAsync(x => x.Id == legacyId);
        Assert.True(await db.WorkoutSessions.AnyAsync(x => x.Id == session.Id));
        Assert.Equal(5, migrated.SuggestedOrder);
    }

    [Fact]
    public async Task Demo_workout_upgrade_does_not_add_a_second_recommendation_for_an_edited_legacy_workout()
    {
        await using var db = CreateDatabase();
        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);
        var upperA = await db.StudentWorkouts.SingleAsync(x => x.StudentId == DemoIds.StudentId && x.Name == "Upper A");
        db.StudentWorkoutExercises.RemoveRange(db.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == upperA.Id));
        db.StudentWorkouts.Remove(upperA);

        var catalog = await db.Exercises.AsNoTracking().ToDictionaryAsync(x => x.Slug);
        var legacyId = Guid.NewGuid();
        var legacy = new StudentWorkout { Id = legacyId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Força · Treino A", Notes = "Edição do usuário", SuggestedOrder = 5, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["agachamento-livre"], 1, 5, 6, 8, 120, "Séries editadas"));
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["supino-reto-com-barra"], 2, 4, 10, 12, 75));
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["remada-baixa"], 3, 4, 8, 12, 90));
        db.StudentWorkouts.Add(legacy);
        await db.SaveChangesAsync();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var restoredUpper = await db.StudentWorkouts.AsNoTracking().SingleAsync(x => x.StudentId == DemoIds.StudentId && x.Name == "Upper A");
        Assert.NotEqual(legacyId, restoredUpper.Id);
        Assert.Equal("Edição do usuário", (await db.StudentWorkouts.AsNoTracking().SingleAsync(x => x.Id == legacyId)).Notes);
    }

    private static PersonalUltraDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<PersonalUltraDbContext>()
            .UseInMemoryDatabase($"exercise-catalog-seed-{Guid.NewGuid():N}")
            .Options;
        return new PersonalUltraDbContext(options);
    }
}
