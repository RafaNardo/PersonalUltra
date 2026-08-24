using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PersonalUltra.Application.Training;

namespace PersonalUltra.Infrastructure;

public static class ExerciseMediaServiceCollectionExtensions
{
    public static IServiceCollection AddExerciseMediaResolver(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ExerciseMediaStorageOptions>()
            .Bind(configuration.GetSection(ExerciseMediaStorageOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ExerciseMediaStorageOptions>, ExerciseMediaStorageOptionsValidator>();
        services.AddSingleton<IExerciseMediaResolver, S3ExerciseMediaResolver>();
        return services;
    }
}
