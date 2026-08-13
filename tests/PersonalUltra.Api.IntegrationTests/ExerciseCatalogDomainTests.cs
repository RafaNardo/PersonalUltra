using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseCatalogDomainTests
{
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
            ImageRef = "assets/exercises/supino-reto-com-barra.jpg",
            Instructions = "Mantenha as escápulas retraídas."
        };

        Assert.True(exercise.IsActive);
        Assert.Equal("Peito", exercise.PrimaryMuscleGroup);
        Assert.Equal("assets/exercises/supino-reto-com-barra.jpg", exercise.ImageRef);
    }
}
