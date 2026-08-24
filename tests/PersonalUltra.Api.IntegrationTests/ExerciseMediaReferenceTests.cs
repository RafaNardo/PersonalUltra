using PersonalUltra.Application.Training;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseMediaReferenceTests
{
    [Fact]
    public void Seeded_media_reference_maps_to_the_exact_bucket_key()
    {
        var reference = ExerciseMediaReference.Parse(
            "media://exercise-catalog/v2/agachamento-com-barra.png");

        Assert.Equal("exercise-catalog/v2/agachamento-com-barra.png", reference.ObjectKey);
    }

    [Theory]
    [InlineData("media://exercise-catalog/v2/../secret.png")]
    [InlineData("media://another-bucket/v2/agachamento.png")]
    [InlineData("media://exercise-catalog/v1/agachamento.png")]
    [InlineData("media://exercise-catalog/v2/Agachamento.png")]
    [InlineData("media://exercise-catalog/v2/agachamento.jpg")]
    [InlineData("media://exercise-catalog/v2/agachamento.png?download=true")]
    public void Unsupported_media_reference_is_rejected(string value)
    {
        Assert.Throws<FormatException>(() => ExerciseMediaReference.Parse(value));
    }
}
