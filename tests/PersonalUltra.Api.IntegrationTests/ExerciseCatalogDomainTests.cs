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

    private static PersonalUltraDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<PersonalUltraDbContext>()
            .UseInMemoryDatabase($"exercise-catalog-seed-{Guid.NewGuid():N}")
            .Options;
        return new PersonalUltraDbContext(options);
    }
}
