using PersonalUltra.ExerciseCatalogFactory.Cli;
using PersonalUltra.ExerciseCatalogFactory.Configuration;

try
{
    var settings = FactorySettings.Load();
    var application = new FactoryApplication(settings, Console.Out, Console.Error);
    return await application.RunAsync(args);
}
catch (Exception exception) when (exception is IOException or InvalidOperationException)
{
    await Console.Error.WriteLineAsync($"Configuração local inválida: {exception.Message}");
    return 2;
}
