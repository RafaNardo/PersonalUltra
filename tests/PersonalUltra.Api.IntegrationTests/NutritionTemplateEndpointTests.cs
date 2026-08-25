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
    public async Task Crud_normalizes_order_lists_counts_duplicates_and_deletes()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);

        var createdResponse = await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("  Base diária  ", twoMeals: true));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(created);
        Assert.Equal("Base diária", created!.Name);
        Assert.Equal([1, 2], created.Meals.Select(x => x.Sequence));
        Assert.Equal(["Café", "Almoço"], created.Meals.Select(x => x.Name));
        Assert.Equal([1, 2], created.Meals[1].Foods.Select(x => x.Sequence));
        Assert.All(created.Meals.SelectMany(x => x.Foods), x => Assert.NotEqual(Guid.Empty, x.Id));

        var summaries = await client.GetFromJsonAsync<List<TemplateSummary>>("/api/v1/nutrition/templates");
        var summary = Assert.Single(summaries!);
        Assert.Equal(2, summary.MealCount);
        Assert.Equal(3, summary.FoodCount);

        var update = await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{created.Id}", Request("Atualizado"));
        Assert.True(update.StatusCode == HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        var updated = await update.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal("Atualizado", updated!.Name);
        Assert.Single(updated.Meals);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);

        var duplicateResponse = await client.PostAsync($"/api/v1/nutrition/templates/{created.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal("Atualizado (cópia)", duplicate!.Name);
        Assert.NotEqual(updated.Id, duplicate.Id);
        Assert.NotEqual(updated.Meals[0].Id, duplicate.Meals[0].Id);
        Assert.NotEqual(updated.Meals[0].Foods[0].Id, duplicate.Meals[0].Foods[0].Id);

        var longName = new string('P', 200);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{duplicate.Id}", Request(longName))).StatusCode);
        var longNameCopyResponse = await client.PostAsync($"/api/v1/nutrition/templates/{duplicate.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.OK, longNameCopyResponse.StatusCode);
        var longNameCopy = await longNameCopyResponse.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal(200, longNameCopy!.Name.Length);
        Assert.EndsWith(" (cópia)", longNameCopy.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/nutrition/templates/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/nutrition/templates/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Trainer_cannot_read_change_duplicate_or_delete_another_trainers_template()
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
            var meal = new NutritionTemplateMeal { Id = Guid.NewGuid(), NutritionTemplateId = id, Name = "Refeição", Notes = "", Sequence = 1 };
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
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/from-template/{id}?replaceExisting=true", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/students/{Guid.NewGuid()}/nutrition/from-template/{id}", null)).StatusCode);
    }

    [Fact]
    public async Task Invalid_update_returns_error_without_mutating_template()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var created = await (await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("Original"))).Content.ReadFromJsonAsync<TemplateResponse>();
        var invalidPayloads = new object?[]
        {
            null,
            new { name = "Preset", notes = "", meals = Array.Empty<object>() },
            new { name = "Preset", notes = "", meals = new[] { new { name = "R", sequence = 1, notes = "", foods = new[] { new { foodName = "Item", quantity = 0m, unit = "g", sequence = 1 } } } } },
            new { name = "Preset", notes = "", meals = new[] { new { name = "R", sequence = 1, notes = "", foods = new[] { new { foodName = "Item", quantity = 1m, unit = "g", sequence = 1 }, new { foodName = "Outro", quantity = 1m, unit = "g", sequence = 1 } } } } },
        };
        foreach (var payload in invalidPayloads)
        {
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/v1/nutrition/templates/{created!.Id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("VALIDATION_ERROR", (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
            var persisted = await client.GetFromJsonAsync<TemplateResponse>($"/api/v1/nutrition/templates/{created.Id}");
            Assert.Equal("Original", persisted!.Name);
        }
    }

    [Fact]
    public async Task Applying_creates_an_independent_snapshot_that_template_edits_do_not_change()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var template = await (await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("Snapshot", twoMeals: true))).Content.ReadFromJsonAsync<TemplateResponse>();
        await RemovePlan(environment);

        var apply = await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/from-template/{template!.Id}", null);
        Assert.True(apply.StatusCode == HttpStatusCode.OK, await apply.Content.ReadAsStringAsync());
        var applied = await apply.Content.ReadFromJsonAsync<AppliedResponse>();
        Assert.Equal(2, applied!.MealCount);
        var originalPlan = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal("Snapshot", originalPlan!.Name);
        Assert.NotEqual(template.Meals[0].Id, originalPlan.Meals[0].Id);
        Assert.NotEqual(template.Meals[0].Foods[0].Id, originalPlan.Meals[0].Foods[0].Id);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/nutrition/templates/{template.Id}", Request("Template alterado"))).StatusCode);
        var unchangedPlan = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal("Snapshot", unchangedPlan!.Name);
        Assert.Equal(2, unchangedPlan.Meals.Count);
    }

    [Fact]
    public async Task Existing_plan_requires_explicit_replace_and_conflict_does_not_mutate()
    {
        await using var environment = new NutritionTestEnvironment();
        var client = Authorized(environment);
        var before = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        var template = await (await client.PostAsJsonAsync("/api/v1/nutrition/templates", Request("Substituto"))).Content.ReadFromJsonAsync<TemplateResponse>();
        var originalCreatorId = Guid.NewGuid();
        await using (var setupScope = environment.TrainerServices.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            setupDb.Trainers.Add(new Trainer { Id = originalCreatorId, Name = "Criador original", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) });
            var existing = await setupDb.NutritionPlans.SingleAsync(x => x.Id == before!.Id);
            existing.CreatedByTrainerId = originalCreatorId;
            await setupDb.SaveChangesAsync();
        }

        var conflict = await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/from-template/{template!.Id}", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("NUTRITION_PLAN_ALREADY_EXISTS", (await conflict.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        var afterConflict = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal(JsonSerializer.Serialize(before, JsonOptions), JsonSerializer.Serialize(afterConflict, JsonOptions));

        var replace = await client.PostAsync($"/api/v1/students/{DemoIds.StudentId}/nutrition/from-template/{template.Id}?replaceExisting=true", null);
        Assert.True(replace.StatusCode == HttpStatusCode.OK, await replace.Content.ReadAsStringAsync());
        var replaced = await client.GetFromJsonAsync<NutritionResponse>($"/api/v1/students/{DemoIds.StudentId}/nutrition");
        Assert.Equal(before!.Id, replaced!.Id);
        Assert.Equal("Substituto", replaced.Name);
        await using var scope = environment.TrainerServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var persisted = await db.NutritionPlans.SingleAsync(x => x.Id == before.Id);
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
        var plan = await db.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods).SingleOrDefaultAsync(x => x.StudentId == DemoIds.StudentId);
        if (plan is not null) db.NutritionPlans.Remove(plan);
        await db.SaveChangesAsync();
    }

    private static object Request(string name, bool twoMeals = false) => new
    {
        name,
        notes = "  Notas  ",
        meals = twoMeals
            ? new object[]
            {
                new { name = "  Almoço  ", sequence = 20, notes = "", foods = new[] { new { foodName = "Arroz", quantity = 100m, unit = "g", sequence = 8 }, new { foodName = "Frango", quantity = 1m, unit = "filé", sequence = 2 } } },
                new { name = "  Café  ", sequence = 10, notes = "", foods = new[] { new { foodName = "Banana", quantity = 1m, unit = "unidade", sequence = 1 } } },
            }
            : new object[] { new { name = "Refeição", sequence = 1, notes = "", foods = new[] { new { foodName = "Item", quantity = 1m, unit = "unidade", sequence = 1 } } } },
    };

    private sealed record ErrorResponse(string Code, string Message, JsonElement Details, string TraceId);
    private sealed record TemplateSummary(Guid Id, string Name, string Notes, int MealCount, int FoodCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
    private sealed record TemplateResponse(Guid Id, string Name, string Notes, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<MealResponse> Meals);
    private sealed record AppliedResponse(Guid Id, Guid StudentId, string Name, DateTimeOffset UpdatedAt, int MealCount);
    private sealed record NutritionResponse(Guid Id, string Name, string Notes, DateTimeOffset UpdatedAt, string ResponsibleTrainerName, IReadOnlyList<MealResponse> Meals);
    private sealed record MealResponse(Guid Id, string Name, int Sequence, string Notes, IReadOnlyList<FoodResponse> Foods);
    private sealed record FoodResponse(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence);
}
