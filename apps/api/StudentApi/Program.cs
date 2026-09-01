using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PersonalUltra.StudentApi.Endpoints;
using PersonalUltra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PersonalUltraDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PersonalUltraDatabase")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddExerciseMediaResolver(builder.Configuration);
builder.Services.AddSingleton<DemoSessionTokenService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, _ => { })
    .AddScheme<AuthenticationSchemeOptions, DemoActorAuthenticationHandler>(DemoActorAuthenticationHandler.StudentScheme, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// A development deployment may intentionally disable the demo seed while
// still needing the current schema (for example, when validating onboarding).
// Keep schema migration independent from demo-data population.
// The demo APIs share a database and must apply the current schema in every
// environment; data population remains controlled independently by the seed flag.
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
    if (app.Configuration.GetValue<bool>("DemoData:SeedOnStartup"))
    {
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(CancellationToken.None);
    }
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Personal Ultra Student API";
        options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
    });
}

app.MapPersonalUltraApi();
app.MapStudentInviteApi();
app.MapAnamnesisApi();
app.MapStudentMessageApi();
app.MapChatApi();
app.MapTrainingApi();
app.MapNutritionProgressApi();
app.MapBrandingApi();
app.MapStudentProfileApi();
app.MapGet("/api/v1/demo/identity", async (PersonalUltraDbContext db, CancellationToken cancellationToken) =>
{
    var student = await db.Students.AsNoTracking().SingleAsync(x => x.Id == PersonalUltra.Domain.DemoIds.StudentId, cancellationToken);
    return Results.Ok(new { actor = "student", id = student.Id, name = $"{student.FirstName} {student.LastName}" });
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { AuthenticationSchemes = DemoActorAuthenticationHandler.StudentScheme });
app.Run();

public partial class Program;
