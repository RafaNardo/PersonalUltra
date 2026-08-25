extern alias trainerapi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class NutritionTemplateEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Crud_normalizes_items_lists_counts_duplicates_and_deletes()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);

        var createdResponse = await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("  Café com ovos  ", twoFoods: true, includeAlternative: true));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(created);
        Assert.Equal("Café com ovos", created!.Name);
        Assert.Equal([1, 2], created.Foods.Select(x => x.Sequence));
        Assert.Equal(["Ovos", "Banana"], created.Foods.Select(x => x.FoodName));
        Assert.Equal("livre", created.Foods[1].Unit);
        var alternative = Assert.Single(created.Foods[1].Alternatives);
        Assert.Equal("Peixe", alternative.FoodName);
        Assert.Equal(200m, alternative.Quantity);

        var summaries = await client.GetFromJsonAsync<List<TemplateSummary>>("/api/v1/nutrition/templates");
        var summary = Assert.Single(summaries!);
        Assert.Equal(2, summary.ItemCount);
        Assert.Equal(["Ovos", "Banana"], summary.FoodNames);

        var update = await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{created.Id}", Request("Café com tapioca"));
        Assert.True(update.StatusCode == HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        var updated = await update.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal("Café com tapioca", updated!.Name);
        Assert.Single(updated.Foods);

        var duplicateResponse = await client.PostAsync($"/api/v1/nutrition/templates/{created.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal("Café com tapioca (cópia)", duplicate!.Name);
        Assert.NotEqual(updated.Id, duplicate.Id);
        Assert.NotEqual(updated.Foods[0].Id, duplicate.Foods[0].Id);

        var longName = new string('P', 200);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{duplicate.Id}", Request(longName))).StatusCode);
        var longNameCopy = await (await client.PostAsync($"/api/v1/nutrition/templates/{duplicate.Id}/duplicate", null)).Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal(200, longNameCopy!.Name.Length);
        Assert.EndsWith(" (cópia)", longNameCopy.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/nutrition/templates/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/nutrition/templates/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Trainer_cannot_access_or_apply_another_trainers_meal_template()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var id = Guid.NewGuid();
        await using (var scope = environment.TrainerServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var otherId = Guid.NewGuid();
            db.Trainers.Add(new Trainer { Id = otherId, Name = "Outro", CreatedAt = DateTimeOffset.UtcNow });
            var template = new NutritionTemplate { Id = id, TrainerId = otherId, Name = "Privado", Notes = "", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var meal = new NutritionTemplateMeal { Id = Guid.NewGuid(), NutritionTemplateId = id, Name = "Privado", Notes = "", Sequence = 1 };
            meal.Foods.Add(new NutritionTemplateFood { Id = Guid.NewGuid(), NutritionTemplateMealId = meal.Id, FoodName = "Item", Quantity = 1, Unit = "unidade", Sequence = 1 });
            template.Meals.Add(meal);
            db.NutritionTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        Assert.Empty((await client.GetFromJsonAsync<List<TemplateSummary>>("/api/v1/nutrition/templates"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/nutrition/templates/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{id}", Request("Tentativa"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/nutrition/templates/{id}/duplicate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/nutrition/templates/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/meals/from-template/{id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/students/{Guid.NewGuid()}/nutrition/meals/from-template/{id}", null)).StatusCode);
    }

    [Fact]
    public async Task Invalid_update_returns_error_without_mutating_meal_template()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var created = await (await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("Original"))).Content.ReadFromJsonAsync<TemplateResponse>();
        var invalidPayloads = new object?[]
        {
            null,
            new { name = "Preset", notes = "", foods = Array.Empty<object>() },
            new { name = "Preset", notes = "", foods = new[] { new { foodName = "Item", quantity = 0m, unit = "g", sequence = 1 } } },
            new { name = "Preset", notes = "", foods = new[] { new { foodName = "Item", quantity = 1m, unit = "g", sequence = 1 }, new { foodName = "Outro", quantity = 1m, unit = "g", sequence = 1 } } },
        };
        foreach (var payload in invalidPayloads)
        {
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/v1/nutrition/templates/{created!.Id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("VALIDATION_ERROR", (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
            Assert.Equal("Original", (await client.GetFromJsonAsync<TemplateResponse>($"/api/v1/nutrition/templates/{created.Id}"))!.Name);
        }
    }

    [Fact]
    public async Task Applying_creates_an_independent_meal_snapshot()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var template = await (await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("Café com ovos", twoFoods: true, includeAlternative: true))).Content.ReadFromJsonAsync<TemplateResponse>();
        await RemovePlan(environment);

        var apply = await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/meals/from-template/{template!.Id}", null);
        Assert.True(apply.StatusCode == HttpStatusCode.OK, await apply.Content.ReadAsStringAsync());
        var applied = await apply.Content.ReadFromJsonAsync<AppliedResponse>();
        Assert.Equal("Café com ovos", applied!.MealName);
        Assert.Equal(1, applied.MealCount);
        var originalPlan = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal("Alimentação de Rafa", originalPlan!.Name);
        Assert.NotEqual(template.Foods[0].Id, originalPlan.Meals[0].Foods[0].Id);
        Assert.Equal("Peixe", Assert.Single(originalPlan.Meals[0].Foods[1].Alternatives).FoodName);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{template.Id}", Request("Café alterado"))).StatusCode);
        var unchangedPlan = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal("Café com ovos", unchangedPlan!.Meals[0].Name);
        Assert.Equal(2, unchangedPlan.Meals[0].Foods.Count);
    }

    [Fact]
    public async Task Applying_to_existing_plan_appends_meal_and_preserves_existing_content_and_creator()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var before = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        var template = await (await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("Ceia rápida"))).Content.ReadFromJsonAsync<TemplateResponse>();
        var originalCreatorId = Guid.NewGuid();
        await using (var setupScope = environment.TrainerServices.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            setupDb.Trainers.Add(new Trainer { Id = originalCreatorId, Name = "Criador original", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) });
            var existing = await setupDb.NutritionPlans.SingleAsync(x => x.Id == before!.Id);
            existing.CreatedByTrainerId = originalCreatorId;
            await setupDb.SaveChangesAsync();
        }

        var apply = await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/meals/from-template/{template!.Id}", null);
        Assert.True(apply.StatusCode == HttpStatusCode.OK, await apply.Content.ReadAsStringAsync());
        var after = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal(before!.Id, after!.Id);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.Meals.Count + 1, after.Meals.Count);
        Assert.Equal(before.Meals.Select(x => x.Name), after.Meals.Take(before.Meals.Count).Select(x => x.Name));
        Assert.Equal("Ceia rápida", after.Meals.Last().Name);

        await using var scope = environment.TrainerServices.CreateAsyncScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>().NutritionPlans.SingleAsync(x => x.Id == before.Id);
        Assert.Equal(originalCreatorId, persisted.CreatedByTrainerId);
        Assert.Equal(DemoIds.TrainerId, persisted.UpdatedByTrainerId);
    }

    private static HttpClient Authorized(NutritionTestEnvironment environment)
    {
        var client = environment.CreateTrainerClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        return client;
    }

    private static async Task RemovePlan(NutritionTestEnvironment environment)
    {
        await using var scope = environment.TrainerServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var plan = await db.NutritionPlans.SingleOrDefaultAsync(x => x.StudentId == DemoIds.StudentId);
        if (plan is not null) db.NutritionPlans.Remove(plan);
        await db.SaveChangesAsync();
    }

    private static object Request(string name, bool twoFoods = false, bool includeAlternative = false) => new
    {
        name,
        notes = "  Opção prática  ",
        foods = twoFoods
            ? new object[]
            {
                includeAlternative
                    ? new { foodName = "  Banana  ", quantity = 1m, unit = "livre", sequence = 8, alternatives = new object[] { new { foodName = "Peixe", quantity = 200m, unit = "g", sequence = 1, notes = "Grelhado" } } }
                    : new { foodName = "  Banana  ", quantity = 1m, unit = "livre", sequence = 8 },
                new { foodName = "  Ovos  ", quantity = 2m, unit = "unidade", sequence = 2 },
            }
            : new object[] { new { foodName = "Item", quantity = 1m, unit = "unidade", sequence = 1 } },
    };

    private sealed record ErrorResponse(string Code, string Message, JsonElement Details, string TraceId);
    private sealed record TemplateSummary(Guid Id, string Name, string Notes, int ItemCount, IReadOnlyList<string> FoodNames, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
    private sealed record TemplateResponse(Guid Id, string Name, string Notes, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<FoodResponse> Foods);
    private sealed record AppliedResponse(Guid PlanId, Guid StudentId, Guid MealId, string MealName, DateTimeOffset UpdatedAt, int MealCount);
    private sealed record NutritionResponse(Guid Id, string Name, string Notes, DateTimeOffset UpdatedAt, string ResponsibleTrainerName, IReadOnlyList<MealResponse> Meals);
    private sealed record MealResponse(Guid Id, string Name, int Sequence, string Notes, IReadOnlyList<FoodResponse> Foods);
    private sealed record FoodResponse(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence, IReadOnlyList<AlternativeResponse> Alternatives);
    private sealed record AlternativeResponse(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence, string Notes);
}
