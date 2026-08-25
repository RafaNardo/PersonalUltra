extern alias trainerapi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class NutritionEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Trainer_and_student_receive_null_when_no_nutrition_plan_exists()
    {
        await using var environment = new NutritionTestEnvironment();
        var trainer = environment.CreateTrainerClient();
        var student = environment.CreateStudentClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await student.GetAsync("/api/v1/nutrition")).StatusCode);
        await LoginStudent(student);
        await using (var scope = environment.TrainerServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.NutritionPlans.RemoveRange(db.NutritionPlans.Where(x => x.StudentId == DemoIds.StudentId));
            await db.SaveChangesAsync();
        }

        var trainerResponse = await trainer.GetAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        var studentResponse = await student.GetAsync("/api/v1/nutrition");

        Assert.Equal(HttpStatusCode.OK, trainerResponse.StatusCode);
        Assert.Equal("null", await trainerResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, studentResponse.StatusCode);
        Assert.Equal("null", await studentResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Trainer_and_student_roundtrip_preserves_order_units_notes_responsibility_and_food_ids()
    {
        await using var environment = new NutritionTestEnvironment();
        var trainer = environment.CreateTrainerClient();
        var student = environment.CreateStudentClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        await LoginStudent(student);

        var request = new
        {
            name = "  Plano flexível  ",
            notes = "  Observação geral  ",
            dailyGoals = new { calories = 2200m, proteinGrams = 140m, carbohydratesGrams = 260m, fatGrams = 70m },
            meals = new object[]
            {
                new
                {
                    name = "  Almoço  ", sequence = 20, notes = "  Preparar antes  ",
                    foods = new object[]
                    {
                        new { foodName = "  Frango  ", quantity = 1.5m, unit = "  filé  ", sequence = 9 },
                        new { foodName = "  Arroz  ", quantity = 120m, unit = "  g  ", sequence = 2 },
                    },
                },
                new
                {
                    name = "  Café da manhã  ", sequence = 10, notes = "  Sem pressa  ",
                    foods = new object[]
                    {
                        new { foodName = "  Café  ", quantity = 1m, unit = "  xícara  ", sequence = 3 },
                        new { foodName = "  Banana  ", quantity = 1m, unit = "  unidade  ", sequence = 1, alternatives = new[] { new { foodName = "  Peixe  ", quantity = 200m, unit = "  g  ", sequence = 1, notes = "  Grelhado  " } } },
                    },
                },
            },
        };

        var saveResponse = await trainer.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition", request);
        Assert.True(saveResponse.StatusCode == HttpStatusCode.OK, await saveResponse.Content.ReadAsStringAsync());
        var trainerPlan = await saveResponse.Content.ReadFromJsonAsync<NutritionResponse>();
        Assert.NotNull(trainerPlan);
        Assert.Equal("Plano flexível", trainerPlan!.Name);
        Assert.Equal("Observação geral", trainerPlan.Notes);
        Assert.Equal(2200m, trainerPlan.DailyGoals!.Calories);
        Assert.Equal(140m, trainerPlan.DailyGoals.ProteinGrams);
        Assert.Equal(260m, trainerPlan.DailyGoals.CarbohydratesGrams);
        Assert.Equal(70m, trainerPlan.DailyGoals.FatGrams);
        Assert.Equal("Severo", trainerPlan.ResponsibleTrainerName);
        Assert.NotEqual(default, trainerPlan.UpdatedAt);
        Assert.Equal([1, 2], trainerPlan.Meals.Select(x => x.Sequence));
        Assert.Equal(["Café da manhã", "Almoço"], trainerPlan.Meals.Select(x => x.Name));
        Assert.Equal([1, 2], trainerPlan.Meals[0].Foods.Select(x => x.Sequence));
        Assert.Equal(["Banana", "Café"], trainerPlan.Meals[0].Foods.Select(x => x.FoodName));
        Assert.Equal(["unidade", "xícara"], trainerPlan.Meals[0].Foods.Select(x => x.Unit));
        var alternative = Assert.Single(trainerPlan.Meals[0].Foods[0].Alternatives);
        Assert.Equal("Peixe", alternative.FoodName);
        Assert.Equal(200m, alternative.Quantity);
        Assert.Equal("Grelhado", alternative.Notes);
        Assert.All(trainerPlan.Meals.SelectMany(x => x.Foods), x => Assert.NotEqual(Guid.Empty, x.Id));

        var studentResponse = await student.GetAsync("/api/v1/nutrition");
        Assert.Equal(HttpStatusCode.OK, studentResponse.StatusCode);
        var studentPlan = await studentResponse.Content.ReadFromJsonAsync<NutritionResponse>();
        Assert.Equal(JsonSerializer.Serialize(trainerPlan, JsonOptions), JsonSerializer.Serialize(studentPlan, JsonOptions));
    }

    [Fact]
    public async Task Trainer_put_is_owned_full_replacement_and_preserves_creator_while_changing_updater()
    {
        await using var environment = new NutritionTestEnvironment();
        var trainer = environment.CreateTrainerClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        var unauthorized = environment.CreateTrainerClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await unauthorized.GetAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition")).StatusCode);
        var unowned = await trainer.PutAsJsonAsync($"/api/v1/students/{Guid.NewGuid()}/nutrition", ValidRequest("Sem vínculo"));
        Assert.Equal(HttpStatusCode.NotFound, unowned.StatusCode);
        Assert.Equal("STUDENT_NOT_FOUND", (await unowned.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);

        var otherTrainerId = Guid.NewGuid();
        Guid creatorId;
        Guid oldMealId;
        Guid[] oldFoodIds;
        await using (var scope = environment.TrainerServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Criador original", CreatedAt = DateTimeOffset.UtcNow.AddDays(-3) });
            var plan = await db.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods).SingleAsync(x => x.StudentId == DemoIds.StudentId);
            plan.TrainerId = otherTrainerId;
            plan.CreatedByTrainerId = otherTrainerId;
            plan.UpdatedByTrainerId = otherTrainerId;
            creatorId = plan.CreatedByTrainerId;
            oldMealId = plan.Meals[0].Id;
            oldFoodIds = plan.Meals.SelectMany(x => x.Foods).Select(x => x.Id).ToArray();
            await db.SaveChangesAsync();
        }

        var replace = await trainer.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition", ValidRequest("Plano substituído"));
        Assert.True(replace.StatusCode == HttpStatusCode.OK, await replace.Content.ReadAsStringAsync());
        var response = await replace.Content.ReadFromJsonAsync<NutritionResponse>();
        Assert.Equal("Severo", response!.ResponsibleTrainerName);
        var onlyMeal = Assert.Single(response.Meals);
        Assert.Single(onlyMeal.Foods);

        await using var verificationScope = environment.TrainerServices.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var persisted = await verificationDb.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods).SingleAsync(x => x.StudentId == DemoIds.StudentId);
        Assert.Equal(creatorId, persisted.CreatedByTrainerId);
        Assert.Equal(DemoIds.TrainerId, persisted.UpdatedByTrainerId);
        Assert.Equal(DemoIds.TrainerId, persisted.TrainerId);
        Assert.DoesNotContain(persisted.Meals, x => x.Id == oldMealId);
        Assert.DoesNotContain(persisted.Meals.SelectMany(x => x.Foods), x => oldFoodIds.Contains(x.Id));
    }

    [Fact]
    public async Task Invalid_destructive_puts_return_error_contract_without_mutating_the_plan()
    {
        await using var environment = new NutritionTestEnvironment();
        var trainer = environment.CreateTrainerClient();
        trainer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        var endpoint = $"/api/v1/students/{DemoIds.StudentId}/nutrition";
        var baseline = await trainer.GetFromJsonAsync<NutritionResponse>(endpoint);
        Assert.NotNull(baseline);

        var validFood = new { foodName = "Arroz", quantity = 100m, unit = "g", sequence = 1 };
        var invalidPayloads = new object?[]
        {
            null,
            new { name = "Plano", notes = "", meals = (object?)null },
            new { name = new string('x', 201), notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { validFood } } } },
            new { name = "Plano", notes = new string('x', 2001), meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { validFood } } } },
            new { name = "Plano", notes = "", dailyGoals = new { calories = -1m }, meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { validFood } } } },
            new { name = "Plano", notes = "", meals = Array.Empty<object>() },
            new { name = "Plano", notes = "", meals = Enumerable.Range(1, 21).Select(i => new { name = $"R{i}", sequence = i, notes = "", foods = new[] { validFood } }).ToArray() },
            new { name = "Plano", notes = "", meals = new object?[] { null } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 0, notes = "", foods = new[] { validFood } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "A", sequence = 1, notes = "", foods = new[] { validFood } }, new { name = "B", sequence = 1, notes = "", foods = new[] { validFood } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = (object?)null } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = Array.Empty<object>() } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = Enumerable.Range(1, 31).Select(i => new { foodName = $"Item {i}", quantity = 1m, unit = "g", sequence = i }).ToArray() } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "Arroz", quantity = 0m, unit = "g", sequence = 1 } } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "Arroz", quantity = 10000.01m, unit = "g", sequence = 1 } } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "Arroz", quantity = 1m, unit = " ", sequence = 1 } } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "Arroz", quantity = 1m, unit = new string('x', 41), sequence = 1 } } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "A", quantity = 1m, unit = "g", sequence = 1 }, new { foodName = "B", quantity = 1m, unit = "g", sequence = 1 } } } } },
            new { name = "Plano", notes = "", meals = new[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "Arroz", quantity = 1m, unit = "g", sequence = 1, alternatives = new[] { new { foodName = "Peixe", quantity = 0m, unit = "g", sequence = 1 } } } } } } },
        };

        foreach (var payload in invalidPayloads)
        {
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            var invalid = await trainer.PutAsync(endpoint, content);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var error = await invalid.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal("VALIDATION_ERROR", error!.Code);
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
            Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
            Assert.Equal(
                JsonSerializer.Serialize(baseline, JsonOptions),
                JsonSerializer.Serialize(await trainer.GetFromJsonAsync<NutritionResponse>(endpoint), JsonOptions));
        }
    }

    private static object ValidRequest(string name) => new
    {
        name,
        notes = "Nova nota",
        meals = new[] { new { name = "Única", sequence = 5, notes = "Nota", foods = new[] { new { foodName = "Água", quantity = 2m, unit = "copos", sequence = 7 } } } },
    };

    private static async Task LoginStudent(HttpClient student)
    {
        var login = await student.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        student.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private sealed record LoginResponse(string AccessToken);
    private sealed record ErrorResponse(string Code, string Message, JsonElement Details, string TraceId);
    private sealed record NutritionResponse(Guid Id, string Name, string Notes, DateTimeOffset UpdatedAt, string ResponsibleTrainerName, IReadOnlyList<MealResponse> Meals, DailyGoalsResponse? DailyGoals = null);
    private sealed record DailyGoalsResponse(decimal? Calories, decimal? ProteinGrams, decimal? CarbohydratesGrams, decimal? FatGrams);
    private sealed record MealResponse(Guid Id, string Name, int Sequence, string Notes, IReadOnlyList<FoodResponse> Foods);
    private sealed record FoodResponse(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence, IReadOnlyList<AlternativeResponse> Alternatives);
    private sealed record AlternativeResponse(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence, string Notes);
}

internal sealed class NutritionTestEnvironment : IAsyncDisposable
{
    private readonly InMemoryDatabaseRoot root = new();
    private readonly string databaseName = $"nutrition-tests-{Guid.NewGuid():N}";
    private NutritionTrainerApiFactory? trainerFactory;
    private NutritionStudentApiFactory? studentFactory;

    public IServiceProvider TrainerServices => trainerFactory!.Services;

    public HttpClient CreateTrainerClient()
    {
        trainerFactory ??= new NutritionTrainerApiFactory(databaseName, root);
        return trainerFactory.CreateClient();
    }

    public HttpClient CreateStudentClient()
    {
        studentFactory ??= new NutritionStudentApiFactory(databaseName, root);
        return studentFactory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        if (studentFactory is not null) await studentFactory.DisposeAsync();
        if (trainerFactory is not null) await trainerFactory.DisposeAsync();
    }
}

internal sealed class NutritionTrainerApiFactory(string databaseName, InMemoryDatabaseRoot root) : WebApplicationFactory<trainerapi::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => Configure(builder, services =>
        services.AddDbContext<PersonalUltraDbContext>(options => options.UseInMemoryDatabase(databaseName, root)));

    internal static void Configure(IWebHostBuilder builder, Action<IServiceCollection> addDbContext)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("RailwayBucket:EndpointUrl", "https://test.storageapi.dev");
        builder.UseSetting("RailwayBucket:Region", "auto");
        builder.UseSetting("RailwayBucket:ForcePathStyle", "false");
        builder.UseSetting("RailwayBucket:SignedUrlLifetimeMinutes", "15");
        builder.UseSetting("RailwayBucket:BucketName", "personal-ultra-tests");
        builder.UseSetting("RailwayBucket:AccessKeyId", "test-access-key");
        builder.UseSetting("RailwayBucket:SecretAccessKey", "test-secret-key");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PersonalUltraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PersonalUltraDbContext>>();
            addDbContext(services);
        });
    }
}

internal sealed class NutritionStudentApiFactory(string databaseName, InMemoryDatabaseRoot root) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => NutritionTrainerApiFactory.Configure(builder, services =>
        services.AddDbContext<PersonalUltraDbContext>(options => options.UseInMemoryDatabase(databaseName, root)));
}
