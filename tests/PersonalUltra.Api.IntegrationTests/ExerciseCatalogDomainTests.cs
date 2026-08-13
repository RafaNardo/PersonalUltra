using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseCatalogDomainTests
{
    [Fact]
    public async Task Demo_seed_creates_a_demonstrable_catalog_with_local_media_references()
    {
        await using var db = CreateDatabase();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var exercises = await db.Exercises.AsNoTracking().ToListAsync();

        Assert.Equal(28, exercises.Count);
        Assert.All(exercises, exercise =>
        {
            Assert.False(string.IsNullOrWhiteSpace(exercise.Name));
            Assert.False(string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup));
            Assert.StartsWith("assets/training/", exercise.ImageRef);
            Assert.EndsWith(".png", exercise.ImageRef);
            Assert.True(exercise.IsActive);
        });
        Assert.Contains(exercises, exercise => exercise.Name == "Supino reto com barra" && exercise.PrimaryMuscleGroup == "Peito");
        Assert.Contains(exercises, exercise => exercise.Name == "Remada baixa" && exercise.PrimaryMuscleGroup == "Costas");
        Assert.Contains(exercises, exercise => exercise.Name == "Desenvolvimento com halteres" && exercise.PrimaryMuscleGroup == "Ombros");
        Assert.Contains(exercises, exercise => exercise.Name == "Rosca direta com barra" && exercise.PrimaryMuscleGroup == "Braços");
        Assert.Contains(exercises, exercise => exercise.Name == "Agachamento livre" && exercise.PrimaryMuscleGroup == "Pernas");
        Assert.Contains(exercises, exercise => exercise.Name == "Elevação pélvica com barra" && exercise.PrimaryMuscleGroup == "Glúteos");
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
    public async Task Demo_seed_image_references_match_the_versioned_donor_asset_manifest()
    {
        await using var db = CreateDatabase();
        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var expected = new[]
        {
            "assets/training/abducao_com_elastico.png",
            "assets/training/abducao_de_quadril_na_maquina.png",
            "assets/training/afundo_com_halteres.png",
            "assets/training/agachamento_goblet.png",
            "assets/training/agachamento_livre.png",
            "assets/training/agachamento_sumo.png",
            "assets/training/cadeira_extensora.png",
            "assets/training/cadeira_flexora.png",
            "assets/training/coice_com_caneleira.png",
            "assets/training/coice_no_cabo.png",
            "assets/training/desenvolvimento-com-halteres.png",
            "assets/training/elevacao-lateral-com-halteres.png",
            "assets/training/elevacao_pelvica_com_barra.png",
            "assets/training/elevacao_pelvica_unilateral_com_barra.png",
            "assets/training/frog_pump.png",
            "assets/training/leg_press_45.png",
            "assets/training/levantamento-terra-romeno.png",
            "assets/training/passada_com_halteres.png",
            "assets/training/ponte_de_gluteo_unilateral.png",
            "assets/training/ponte_de_gluteos.png",
            "assets/training/pull_through_no_cabo.png",
            "assets/training/puxada-dorsal-na-maquina.png",
            "assets/training/remada-baixa.png",
            "assets/training/rosca-direta-com-barra.png",
            "assets/training/step_up_com_halteres.png",
            "assets/training/stiff_com_barra.png",
            "assets/training/supino-reto-com-barra.png",
            "assets/training/triceps-na-polia-com-corda.png",
        };

        var actual = await db.Exercises.AsNoTracking().Select(x => x.ImageRef).OrderBy(x => x).ToArrayAsync();

        Assert.Equal(expected.OrderBy(x => x), actual);
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
            .OrderBy(x => x.RecommendedDay)
            .ToListAsync();

        Assert.Equal(["Upper A", "Lower A", "Upper B", "Lower B"], workouts.Select(x => x.Name));
        Assert.Equal([1, 2, 4, 5], workouts.Select(x => x.RecommendedDay));
        Assert.Equal([1, 2, 3, 4], workouts.Select(x => x.SuggestedOrder));
        Assert.All(workouts, workout =>
        {
            Assert.Equal(6, workout.Exercises.Count);
            Assert.Equal([1, 2, 3, 4, 5, 6], workout.Exercises.OrderBy(x => x.Sequence).Select(x => x.Sequence));
            Assert.All(workout.Exercises, exercise =>
            {
                Assert.NotNull(exercise.ExerciseId);
                Assert.StartsWith("assets/training/", exercise.ImageRef);
                Assert.DoesNotContain("mock", exercise.ImageRef!, StringComparison.OrdinalIgnoreCase);
                Assert.False(string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup));
                Assert.True(exercise.RepetitionsMin <= exercise.RepetitionsMax);
                Assert.InRange(exercise.Sets, 1, 20);
            });
        });
        Assert.Single(workouts, x => x.IsRecommended);
        Assert.Equal("Upper A", workouts.Single(x => x.IsRecommended).Name);
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
        var custom = new StudentWorkout { Id = customId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Treino personalizado", Notes = "Edição do aluno", RecommendedDay = 3, IsRecommended = false, CreatedAt = DateTimeOffset.UtcNow };
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
        var legacy = new StudentWorkout { Id = legacyId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Força · Treino A", Notes = "Foco em execução consistente e progressão gradual.", RecommendedDay = 1, IsRecommended = true, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
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
        Assert.False(migrated.IsRecommended);
        Assert.True(await db.WorkoutSessions.AnyAsync(x => x.Id == session.Id));
        Assert.Single(await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == DemoIds.StudentId && x.IsRecommended).ToListAsync());
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
        var legacy = new StudentWorkout { Id = legacyId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Força · Treino A", Notes = "Edição do usuário", RecommendedDay = 1, IsRecommended = true, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["agachamento-livre"], 1, 5, 6, 8, 120, "Séries editadas"));
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["supino-reto-com-barra"], 2, 4, 10, 12, 75));
        legacy.Exercises.Add(StudentWorkoutExercise.FromCatalog(legacyId, catalog["remada-baixa"], 3, 4, 8, 12, 90));
        db.StudentWorkouts.Add(legacy);
        await db.SaveChangesAsync();

        await new DemoDataSeeder(db, TimeProvider.System).SeedAsync(CancellationToken.None);

        var recommended = await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == DemoIds.StudentId && x.IsRecommended).ToListAsync();
        Assert.Single(recommended);
        Assert.Equal(legacyId, recommended[0].Id);
        var restoredUpper = await db.StudentWorkouts.AsNoTracking().SingleAsync(x => x.StudentId == DemoIds.StudentId && x.Name == "Upper A");
        Assert.False(restoredUpper.IsRecommended);
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
