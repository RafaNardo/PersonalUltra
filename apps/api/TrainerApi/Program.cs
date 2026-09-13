using Microsoft.EntityFrameworkCore;
using PersonalUltra.TrainerApi.Endpoints;
using PersonalUltra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PersonalUltraDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PersonalUltraDatabase")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddExerciseMediaResolver(builder.Configuration);
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddClerkAuthentication(builder.Configuration, ClerkActor.Trainer);
var app = builder.Build();
// The demo APIs share a database and must apply the current schema in every
// environment; data population remains controlled independently by the seed flag.
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
    if (db.Database.IsRelational()) await db.Database.MigrateAsync(); else await db.Database.EnsureCreatedAsync();
    if (app.Configuration.GetValue<bool>("DemoData:SeedOnStartup")) await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(CancellationToken.None);
}
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { actor = "trainer" }));
app.MapDashboardApi();
app.MapTrainerOnboardingApi();
app.MapStudentApi();
app.MapStudentInviteApi();
app.MapTrainingApi();
app.MapNutritionProgressApi();
app.MapNutritionTemplateApi();
app.MapDemoResetApi();
app.MapBrandingApi();
app.MapTrainerSettingsApi();
app.Run();

public partial class Program;
