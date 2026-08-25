using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using PersonalUltra.TrainerApi.Endpoints;
using PersonalUltra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PersonalUltraDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PersonalUltraDatabase")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddExerciseMediaResolver(builder.Configuration);
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddAuthentication(DemoActorAuthenticationHandler.TrainerScheme)
    .AddScheme<AuthenticationSchemeOptions, DemoActorAuthenticationHandler>(DemoActorAuthenticationHandler.TrainerScheme, _ => { });
builder.Services.AddAuthorization();
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
app.MapGet("/api/v1/demo/identity", async (PersonalUltraDbContext db, CancellationToken cancellationToken) =>
{
    var trainer = await db.Trainers.AsNoTracking().SingleAsync(x => x.Id == PersonalUltra.Domain.DemoIds.TrainerId, cancellationToken);
    return Results.Ok(new { actor = "trainer", id = trainer.Id, name = trainer.Name });
}).RequireAuthorization();
app.MapDashboardApi();
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
