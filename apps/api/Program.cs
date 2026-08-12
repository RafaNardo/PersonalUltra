using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PersonalUltra.Api.Application.Nutrition;
using PersonalUltra.Api.Application.Training;
using PersonalUltra.Api.Application.Safety;
using PersonalUltra.Api.Endpoints;
using PersonalUltra.Api.Application.Coach;
using PersonalUltra.Api.Application.Plans;
using PersonalUltra.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PersonalUltraDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PersonalUltraDatabase")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DemoSessionTokenService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<MemberDemoResetService>();
builder.Services.AddScoped<StandardPlanProvisioner>();
builder.Services.AddSingleton<FoodAlternativesEngine>();
builder.Services.AddSingleton<ExerciseAlternativesEngine>();
builder.Services.AddSingleton<PainSafetyEngine>();
builder.Services.AddScoped<CoachContextBuilder>();
builder.Services.AddSingleton<CoachOutputValidator>();
builder.Services.AddSingleton<ExerciseSubstitutionTool>();
builder.Services.Configure<OpenAiCoachOptions>(builder.Configuration.GetSection(OpenAiCoachOptions.SectionName));
builder.Services.PostConfigure<OpenAiCoachOptions>(options => options.ApiKey ??= builder.Configuration["ai-api-key"]);
builder.Services.AddHttpClient<OpenAiCoachResponder>(client => client.Timeout = TimeSpan.FromSeconds(12));
builder.Services.AddSingleton<DeterministicCoachResponder>();
builder.Services.AddScoped<ICoachResponder, ResilientCoachResponder>();
builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// A development deployment may intentionally disable the demo seed while
// still needing the current schema (for example, when validating onboarding).
// Keep schema migration independent from demo-data population.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("DemoData:SeedOnStartup"))
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
        options.Title = "SVR Method API";
        options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
    });
}

app.MapPersonalUltraApi();
app.MapOnboardingApi();
app.MapInitialPlanApi();
app.MapM1Api();
app.Run();

public partial class Program;
