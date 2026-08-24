namespace PersonalUltra.Application.Training;

/// <summary>
/// Converts stable exercise media references into values suitable for HTTP responses.
/// Implementations must not mutate the persisted reference.
/// </summary>
public interface IExerciseMediaResolver
{
    string? ResolveUrl(string? imageRef);
}
