using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using SvrMethod.Api.Contracts;
using SvrMethod.Api.Application.Coach;
using SvrMethod.Api.Domain;
using SvrMethod.Api.Infrastructure;

namespace SvrMethod.Api.IntegrationTests;

public sealed class WorkoutFlowTests : IClassFixture<SvrApiFactory>
{
    private readonly HttpClient _client;
    private readonly SvrApiFactory _factory;

    public WorkoutFlowTests(SvrApiFactory factory)
    {
        _factory = factory;
        factory.Seed();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task New_member_can_resume_and_complete_a_descriptive_onboarding_profile()
    {
        var email = $"onboarding-{Guid.NewGuid():N}@example.test";
        var login = await _client.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest(email));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var credentials = await login.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(credentials);
        Assert.True(credentials!.IsNewMember);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credentials.TokenType, credentials.AccessToken);

        var initial = await _client.GetFromJsonAsync<OnboardingProfileDto>("/api/v1/onboarding/profile");
        Assert.NotNull(initial);
        Assert.Equal(1, initial!.CurrentStep);

        var identityOnly = await _client.PutAsJsonAsync("/api/v1/onboarding/profile", new
        {
            FirstName = "Rafaela",
            LastName = "Silva",
            CurrentStep = 1
        });
        Assert.Equal(HttpStatusCode.OK, identityOnly.StatusCode);

        var draft = new SaveOnboardingProfileRequest("Rafaela", "Silva", "Ganhar massa", "Iniciante", 4, 60,
            "Academia", "Halteres, máquinas e elásticos", 165, 64.5m, "Nenhuma informada", "Nenhuma informada",
            "Sem dor atual", "Prefiro refeições simples", "Nenhuma informada", 4);
        var saved = await _client.PutAsJsonAsync("/api/v1/onboarding/profile", draft);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var resumed = await _client.GetFromJsonAsync<OnboardingProfileDto>("/api/v1/onboarding/profile");
        Assert.Equal("Rafaela", resumed!.FirstName);
        Assert.Equal(4, resumed.CurrentStep);

        var completed = await _client.PostAsync("/api/v1/onboarding/complete", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.True((await completed.Content.ReadFromJsonAsync<OnboardingProfileDto>())!.IsCompleted);
        var bootstrap = await _client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.Equal("PreparePlan", bootstrap!.NextRoute);
    }

    [Fact]
    public async Task Completed_member_receives_an_owned_complete_standard_plan_idempotently()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        var member = await AuthenticateNewCompletedMemberAsync(client);

        var created = await client.PostAsync("/api/v1/plans/initial", null);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var first = await created.Content.ReadFromJsonAsync<InitialPlanResponse>();
        Assert.NotNull(first);
        Assert.True(first!.IsProvisioned);
        Assert.False(first.WasAlreadyProvisioned);
        Assert.Equal(4, first.Workouts.Count);
        Assert.NotNull(first.Nutrition);
        Assert.Equal(6, first.Nutrition!.Meals.Count);

        var repeated = await client.PostAsync("/api/v1/plans/initial", null);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var replay = await repeated.Content.ReadFromJsonAsync<InitialPlanResponse>();
        Assert.NotNull(replay);
        Assert.True(replay!.WasAlreadyProvisioned);
        Assert.Equal(first.PlanId, replay.PlanId);

        await using var scope = isolatedFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
        var plan = await db.Plans.Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates).ThenInclude(x => x.Exercises)
            .Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates).ThenInclude(x => x.WorkoutSessions)
            .Include(x => x.Member).SingleAsync(x => x.Id == first.PlanId);
        var nutrition = await db.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods).SingleAsync(x => x.PlanId == plan.Id);

        Assert.Equal(member.Id, plan.MemberId);
        Assert.NotEqual(DemoIds.MemberId, plan.MemberId);
        Assert.Equal(4, plan.TrainingPlan.WorkoutTemplates.Count);
        Assert.All(plan.TrainingPlan.WorkoutTemplates, item => Assert.InRange(item.Exercises.Count, 6, 7));
        Assert.Equal(6, nutrition.Meals.Count);
        Assert.True(await db.WorkoutSessions.AnyAsync(x => x.MemberId == member.Id && x.Status == "Planned"));
        Assert.True(await db.WorkoutSessions.CountAsync(x => x.MemberId == member.Id && x.Status == "Completed") >= 20);
        Assert.Equal(9, await db.WeightEntries.CountAsync(x => x.MemberId == member.Id));
        Assert.True(await db.SetPerformances.AnyAsync(x => x.WorkoutSessionExercise.WorkoutSession.MemberId == member.Id));
        Assert.Single(await db.Plans.Where(x => x.MemberId == member.Id && x.Status == "Active").ToListAsync());
    }

    [Fact]
    public async Task Initial_plan_is_blocked_until_onboarding_is_completed_and_is_member_scoped()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var firstClient = isolatedFactory.CreateClient();
        var firstLogin = await firstClient.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest($"plan-first-{Guid.NewGuid():N}@example.test"));
        var firstCredentials = (await firstLogin.Content.ReadFromJsonAsync<DevLoginResponse>())!;
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(firstCredentials.TokenType, firstCredentials.AccessToken);

        var blocked = await firstClient.PostAsync("/api/v1/plans/initial", null);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("ONBOARDING_INCOMPLETE", (await blocked.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.Conflict, (await firstClient.GetAsync("/api/v1/plans/initial")).StatusCode);

        await AuthenticateNewCompletedMemberAsync(firstClient, firstCredentials.Member.Email);
        var provisioned = await firstClient.PostAsync("/api/v1/plans/initial", null);
        Assert.Equal(HttpStatusCode.Created, provisioned.StatusCode);

        using var secondClient = isolatedFactory.CreateClient();
        var second = await AuthenticateNewCompletedMemberAsync(secondClient);
        var otherMemberPlan = await secondClient.GetFromJsonAsync<InitialPlanResponse>("/api/v1/plans/initial");
        Assert.NotNull(otherMemberPlan);
        Assert.False(otherMemberPlan!.IsProvisioned);
        Assert.NotEqual(firstCredentials.Member.Id, second.Id);
    }

    [Fact]
    public async Task Member_demo_reset_deletes_only_the_authenticated_member_and_allows_a_fresh_login()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var memberClient = isolatedFactory.CreateClient();
        const string email = "restartable-member@example.test";
        var member = await AuthenticateNewCompletedMemberAsync(memberClient, email);
        Assert.Equal(HttpStatusCode.Created, (await memberClient.PostAsync("/api/v1/plans/initial", null)).StatusCode);

        using var otherClient = isolatedFactory.CreateClient();
        var other = await AuthenticateNewCompletedMemberAsync(otherClient, "another-member@example.test");
        Assert.Equal(HttpStatusCode.Created, (await otherClient.PostAsync("/api/v1/plans/initial", null)).StatusCode);

        var reset = await memberClient.PostAsync("/api/v1/demo/member-reset", null);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await memberClient.GetAsync("/api/v1/bootstrap")).StatusCode);

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            Assert.False(await db.Members.AnyAsync(x => x.Id == member.Id));
            Assert.False(await db.AuthUsers.AnyAsync(x => x.Email == email));
            Assert.False(await db.Plans.AnyAsync(x => x.MemberId == member.Id));
            Assert.False(await db.WorkoutSessions.AnyAsync(x => x.MemberId == member.Id));
            Assert.True(await db.Members.AnyAsync(x => x.Id == other.Id));
            Assert.True(await db.Plans.AnyAsync(x => x.MemberId == other.Id && x.Status == "Active"));
            Assert.True(await db.Members.AnyAsync(x => x.Id == DemoIds.MemberId));
            Assert.NotEmpty(await db.Foods.ToListAsync());
            Assert.NotEmpty(await db.MethodologyVersions.ToListAsync());
        }

        using var freshLoginClient = isolatedFactory.CreateClient();
        var freshLogin = await freshLoginClient.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest(email));
        var credentials = await freshLogin.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.Equal(HttpStatusCode.OK, freshLogin.StatusCode);
        Assert.NotNull(credentials);
        Assert.True(credentials!.IsNewMember);
        Assert.NotEqual(member.Id, credentials.Member.Id);
    }

    [Fact]
    public async Task Demo_seed_creates_a_valid_nutrition_plan_and_catalog()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();

        var nutrition = await db.NutritionPlans
            .Include(x => x.Meals)
            .ThenInclude(x => x.Foods)
            .ThenInclude(x => x.Food)
            .SingleAsync(x => x.Plan.MemberId == DemoIds.MemberId);

        Assert.Equal(6, nutrition.Meals.Count);
        Assert.Equal(Enumerable.Range(1, 6), nutrition.Meals.OrderBy(x => x.Sequence).Select(x => x.Sequence));
        Assert.All(nutrition.Meals.SelectMany(x => x.Foods), item => Assert.True(item.QuantityGrams > 0));
        Assert.All(nutrition.Meals.SelectMany(x => x.Foods).Select(x => x.Food), food =>
        {
            Assert.True(food.CaloriesPer100g > 0);
            Assert.True(food.ProteinPer100g >= 0);
            Assert.True(food.CarbsPer100g >= 0);
            Assert.True(food.FatPer100g >= 0);
        });
        Assert.Single(await db.DailyLogs.Where(x => x.MemberId == DemoIds.MemberId && x.Completed).ToListAsync());
    }

    [Fact]
    public void Nutrition_model_enforces_plan_and_meal_template_uniqueness()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();

        var model = db.GetInfrastructure().GetRequiredService<IDesignTimeModel>().Model;
        var nutritionPlan = model.FindEntityType(typeof(NutritionPlan));
        var mealTemplate = model.FindEntityType(typeof(MealTemplate));

        Assert.NotNull(nutritionPlan);
        Assert.NotNull(mealTemplate);
        Assert.Contains(nutritionPlan!.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(NutritionPlan.PlanId)]));
        Assert.Contains(mealTemplate!.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(MealTemplate.NutritionPlanId), nameof(MealTemplate.Sequence)]));
        Assert.NotEmpty(nutritionPlan.GetCheckConstraints());
        Assert.NotEmpty(mealTemplate.GetCheckConstraints());
    }

    [Fact]
    public async Task Demo_user_can_complete_a_workout_and_replay_a_set_operation()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/dev-login", new { });
        login.EnsureSuccessStatusCode();
        var credentials = await login.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(credentials);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credentials.TokenType, credentials.AccessToken);

        var bootstrap = await _client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        Assert.Equal("Home", bootstrap.NextRoute);

        var today = await _client.GetFromJsonAsync<TrainingTodayResponse>("/api/v1/training/today");
        Assert.NotNull(today);
        var started = await _client.PostAsync($"/api/v1/training/sessions/{today.Id}/start", null);
        var start = await started.Content.ReadFromJsonAsync<StartWorkoutResponse>();
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.NotNull(start);
        Assert.False(start.WasAlreadyStarted);

        var exercise = Assert.Single(today.Exercises, x => x.Sequence == 1);
        var operationId = Guid.NewGuid();
        var request = new CompleteSetRequest(operationId, 1, exercise.RecommendedLoadKg, exercise.MinimumRepetitions, 2);
        var setEndpoint = $"/api/v1/training/sessions/{today.Id}/exercises/{exercise.Id}/sets";
        var firstSet = await _client.PostAsJsonAsync(setEndpoint, request);
        var firstResponse = await firstSet.Content.ReadFromJsonAsync<CompleteSetResponse>();
        Assert.Equal(HttpStatusCode.Created, firstSet.StatusCode);
        Assert.NotNull(firstResponse);
        Assert.False(firstResponse.WasAlreadyProcessed);

        var replay = await _client.PostAsJsonAsync(setEndpoint, request);
        var replayResponse = await replay.Content.ReadFromJsonAsync<CompleteSetResponse>();
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.NotNull(replayResponse);
        Assert.True(replayResponse.WasAlreadyProcessed);
        Assert.Equal(firstResponse.Id, replayResponse.Id);

        var completed = await _client.PostAsync($"/api/v1/training/sessions/{today.Id}/complete", null);
        var completion = await completed.Content.ReadFromJsonAsync<CompleteWorkoutResponse>();
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.NotNull(completion);
        Assert.Equal("Completed", completion.Status);
        Assert.Equal(1, completion.CompletedSets);
    }

    [Fact]
    public async Task Development_environment_exposes_swagger_document_and_scalar()
    {
        var openApi = await _client.GetAsync("/swagger/v1/swagger.json");
        var scalar = await _client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
    }

    [Fact]
    public async Task Development_email_identity_creates_or_recovers_an_isolated_member_and_bootstraps_lifecycle()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        const string email = "nova.aluna@svr.method";

        var createdResponse = await client.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest(email));
        Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(created);
        Assert.True(created!.IsNewMember);
        Assert.Equal(email, created.Member.Email);
        Assert.NotEqual(DemoIds.MemberId, created.Member.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(created.TokenType, created.AccessToken);
        var onboarding = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(onboarding);
        Assert.Equal("Onboarding", onboarding!.NextRoute);
        Assert.Null(onboarding.ActivePlan);

        var repeatedResponse = await client.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest(" NOVA.ALUNA@SVR.METHOD "));
        var repeated = await repeatedResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(repeated);
        Assert.False(repeated!.IsNewMember);
        Assert.Equal(created.Member.Id, repeated.Member.Id);

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            var member = await db.Members.SingleAsync(member => member.Id == created.Member.Id);
            member.OnboardingCompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(repeated.TokenType, repeated.AccessToken);
        var preparing = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(preparing);
        Assert.Equal("PreparePlan", preparing!.NextRoute);
    }

    [Theory]
    [InlineData("not-an-email")]
    public async Task Development_email_identity_rejects_invalid_emails(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest(email));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Demo_user_can_read_the_complete_training_plan_and_prescriptions()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var plan = await client.GetFromJsonAsync<TrainingPlanResponse>("/api/v1/training/plan");

        Assert.NotNull(plan);
        Assert.Equal("SVR Foco em Glúteos e Pernas 4x", plan!.Name);
        Assert.Equal(4, plan.SessionsPerWeek);
        Assert.Equal(["Superior — costas e braços", "Glúteos 1", "Pernas — quadríceps e glúteos", "Posteriores e glúteos"], plan.Workouts.Select(workout => workout.Name));
        Assert.All(plan.Workouts, workout => Assert.InRange(workout.Exercises.Count, 6, 7));
        Assert.All(plan.Workouts.SelectMany(workout => workout.Exercises), exercise => Assert.True(exercise.RecommendedLoadKg > 0));
    }

    [Fact]
    public async Task Demo_user_can_log_a_valid_weight_and_invalid_weight_is_rejected()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/dev-login", new { });
        var credentials = await login.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(credentials);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credentials!.TokenType, credentials.AccessToken);

        var recordedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var created = await _client.PostAsJsonAsync("/api/v1/progress/weight", new CreateWeightRequest(81.25m, recordedAt));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var entry = await created.Content.ReadFromJsonAsync<WeightDto>();
        Assert.NotNull(entry);
        Assert.Equal(81.25m, entry!.WeightKg);
        Assert.Equal(recordedAt, entry.RecordedAt);

        var history = await _client.GetFromJsonAsync<List<WeightDto>>("/api/v1/progress/weight");
        Assert.NotNull(history);
        Assert.Contains(history!, item => item.Id == entry.Id && item.WeightKg == 81.25m && item.RecordedAt == recordedAt);
        Assert.True(history!.SequenceEqual(history.OrderBy(item => item.RecordedAt)));

        var invalid = await _client.PostAsJsonAsync("/api/v1/progress/weight", new CreateWeightRequest(24.99m, null));

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("VALIDATION_ERROR", await invalid.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Demo_user_can_use_m1_progress_nutrition_and_coach_safety_flows()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/dev-login", new { });
        var credentials = await login.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(credentials);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credentials!.TokenType, credentials.AccessToken);

        var progress = await _client.GetFromJsonAsync<ProgressSummaryDto>("/api/v1/progress/summary");
        Assert.NotNull(progress);
        Assert.True(progress.CurrentWeightKg > 0);
        var weights = await _client.GetFromJsonAsync<List<WeightDto>>("/api/v1/progress/weight");
        Assert.NotNull(weights);
        Assert.True(weights.Count >= 9);
        Assert.Equal(weights.OrderBy(x => x.RecordedAt).Last().WeightKg, progress.CurrentWeightKg);
        Assert.InRange(progress.ConsistencyPercent, 0, 100);
        Assert.True(progress.DaysOnMethod >= 28);
        Assert.NotNull(progress.Strength);
        Assert.True(progress.Strength.ChangePercent > 0);
        var weight = await _client.PostAsJsonAsync("/api/v1/progress/weight", new CreateWeightRequest(81.2m, null));
        Assert.Equal(HttpStatusCode.Created, weight.StatusCode);

        var nutrition = await _client.GetFromJsonAsync<NutritionTodayDto>("/api/v1/nutrition/today");
        Assert.NotNull(nutrition);
        Assert.Equal(6, nutrition.Meals.Count);
        var breakfast = Assert.Single(nutrition.Meals, item => item.Name == "Café da manhã");
        Assert.Contains(breakfast.Foods, food => food.Name == "Ovo cozido");
        Assert.DoesNotContain(breakfast.Foods, food => food.Name == "Frango grelhado");
        var lunch = Assert.Single(nutrition.Meals, item => item.Name == "Almoço");
        var chicken = Assert.Single(lunch.Foods, food => food.Name == "Frango grelhado");
        var alternatives = await _client.GetFromJsonAsync<List<FoodAlternativeDto>>($"/api/v1/nutrition/meals/{lunch.Id}/foods/{chicken.FoodId}/alternatives");
        Assert.Contains(alternatives!, food => food.Name == "Patinho moído");
        var meal = nutrition.Meals.FirstOrDefault();
        Assert.NotNull(meal);
        var completeMeal = await _client.PostAsync($"/api/v1/nutrition/meals/{meal.Id}/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, completeMeal.StatusCode);

        var pain = await _client.PostAsJsonAsync("/api/v1/health/pain-reports", new PainReportRequest("Joelho", "Direito", 8, "Agachamento"));
        var painResponse = await pain.Content.ReadFromJsonAsync<PainReportDto>();
        Assert.Equal(HttpStatusCode.OK, pain.StatusCode);
        Assert.Equal("Red", painResponse!.SafetyLevel);

        var coach = await _client.PostAsJsonAsync("/api/v1/coach/messages", new SendCoachMessageRequest("Quero trocar um exercício"));
        var coachResponse = await coach.Content.ReadFromJsonAsync<CoachConversationDto>();
        Assert.Equal(HttpStatusCode.OK, coach.StatusCode);
        Assert.Contains(coachResponse!.Messages, message => message.Kind == "Choice" && message.MetadataJson!.Contains("EXERCISE_SELECTION_REQUIRED"));
        var foodCoach = await _client.PostAsJsonAsync("/api/v1/coach/messages", new SendCoachMessageRequest("Consigo trocar o arroz do almoço?"));
        var foodCoachResponse = await foodCoach.Content.ReadFromJsonAsync<CoachConversationDto>();
        Assert.Contains(foodCoachResponse!.Messages, message => message.Kind == "Text" && message.Content.Contains("abra Nutrição"));
    }

    [Fact]
    public async Task Coach_conversation_is_created_once_scoped_to_member_and_returns_messages_in_order()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            var demoConversation = await db.Conversations.SingleAsync(x => x.MemberId == DemoIds.MemberId);
            db.CoachMessages.RemoveRange(db.CoachMessages.Where(x => x.ConversationId == demoConversation.Id));
            db.Conversations.Remove(demoConversation);
            await db.SaveChangesAsync();
        }

        var created = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        var repeated = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        Assert.NotNull(created);
        Assert.NotNull(repeated);
        Assert.Equal(created!.Id, repeated!.Id);
        Assert.Empty(created.Messages);

        var firstMessageId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondMessageId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            var timestamp = DateTimeOffset.UtcNow.AddMinutes(-1);
            db.CoachMessages.AddRange(
                new CoachMessage { Id = secondMessageId, ConversationId = created.Id, Role = "Assistant", Kind = "Text", Content = "Segunda mensagem", CreatedAt = timestamp },
                new CoachMessage { Id = firstMessageId, ConversationId = created.Id, Role = "User", Kind = "Text", Content = "Primeira mensagem", CreatedAt = timestamp });

            var foreignUser = new AuthUser { Id = Guid.NewGuid(), Email = "coach-isolation@svr.method", CreatedAt = timestamp };
            var foreignMember = new Member { Id = Guid.NewGuid(), AuthUserId = foreignUser.Id, FirstName = "Outra", LastName = "Pessoa", CreatedAt = timestamp };
            var foreignConversation = new Conversation { Id = Guid.NewGuid(), MemberId = foreignMember.Id, CreatedAt = timestamp };
            foreignConversation.Messages.Add(new CoachMessage { Id = Guid.NewGuid(), Role = "User", Kind = "Text", Content = "Mensagem de outra pessoa", CreatedAt = timestamp });
            db.AddRange(foreignUser, foreignMember, foreignConversation);
            await db.SaveChangesAsync();
        }

        var conversation = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        Assert.NotNull(conversation);
        Assert.Equal(created.Id, conversation!.Id);
        Assert.Equal(["Primeira mensagem", "Segunda mensagem"], conversation.Messages.Select(x => x.Content));
        Assert.DoesNotContain(conversation.Messages, message => message.Content == "Mensagem de outra pessoa");

        await using var modelScope = isolatedFactory.Services.CreateAsyncScope();
        var modelDb = modelScope.ServiceProvider.GetRequiredService<SvrDbContext>();
        var model = modelDb.GetInfrastructure().GetRequiredService<IDesignTimeModel>().Model;
        var conversationEntity = model.FindEntityType(typeof(Conversation));
        var messageEntity = model.FindEntityType(typeof(CoachMessage));
        Assert.Contains(conversationEntity!.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(Conversation.MemberId)]));
        Assert.Contains(messageEntity!.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(CoachMessage.ConversationId), nameof(CoachMessage.CreatedAt), nameof(CoachMessage.Id)]));
    }

    [Fact]
    public async Task Coach_context_contains_only_member_scoped_safe_summaries()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();

        await using var scope = isolatedFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
        var builder = scope.ServiceProvider.GetRequiredService<CoachContextBuilder>();

        var context = await builder.BuildAsync(DemoIds.MemberId, CancellationToken.None);

        Assert.Equal("Rafa", context.Member.FirstName);
        Assert.NotNull(context.ActivePlan);
        Assert.Equal("SVR Foco em Glúteos e Pernas 4x", context.ActivePlan!.Name);
        Assert.Equal(4, context.ActivePlan.SessionsPerWeek);
        Assert.NotNull(context.TodayWorkout);
        Assert.Equal(7, context.TodayWorkout!.ExerciseCount);
        var templates = await db.WorkoutTemplates.Include(template => template.Exercises).Where(template => template.TrainingPlan.Plan.MemberId == DemoIds.MemberId).OrderBy(template => template.Sequence).ToListAsync();
        Assert.Equal(["Superior — costas e braços", "Glúteos 1", "Pernas — quadríceps e glúteos", "Posteriores e glúteos"], templates.Select(template => template.Name));
        Assert.All(templates, template => Assert.InRange(template.Exercises.Count, 6, 7));
        Assert.NotNull(context.TodayNutrition);
        Assert.Equal(2600, context.TodayNutrition!.CaloriesTarget);
        Assert.Equal(6, context.TodayNutrition.MealsTotal);
        Assert.True(context.Progress.CompletedWorkouts > 0);
        Assert.True(context.Progress.CurrentWeightKg > 0);
        Assert.False(context.Safety.HasRecentPain);
        Assert.Null(context.Safety.MostRecentPainSafetyLevel);

        var timestamp = DateTimeOffset.UtcNow;
        db.PainReports.Add(new PainReport { Id = Guid.NewGuid(), MemberId = DemoIds.MemberId, Area = "Joelho", Side = "Direito", Intensity = 6, Context = "Agachamento", SafetyLevel = "Yellow", ReportedAt = timestamp });
        var foreignUser = new AuthUser { Id = Guid.NewGuid(), Email = "context-isolation@svr.method", CreatedAt = timestamp };
        var foreignMember = new Member { Id = Guid.NewGuid(), AuthUserId = foreignUser.Id, FirstName = "Outra", LastName = "Pessoa", CreatedAt = timestamp };
        db.AddRange(foreignUser, foreignMember);
        await db.SaveChangesAsync();

        var safetyContext = await builder.BuildAsync(DemoIds.MemberId, CancellationToken.None);
        var foreignContext = await builder.BuildAsync(foreignMember.Id, CancellationToken.None);

        Assert.True(safetyContext.Safety.HasRecentPain);
        Assert.Equal("Yellow", safetyContext.Safety.MostRecentPainSafetyLevel);
        Assert.Equal("Outra", foreignContext.Member.FirstName);
        Assert.Null(foreignContext.ActivePlan);
        Assert.Null(foreignContext.TodayWorkout);
        Assert.Null(foreignContext.TodayNutrition);
        Assert.Null(foreignContext.Progress.CurrentWeightKg);
        Assert.Equal(0, foreignContext.Progress.CompletedWorkouts);
        Assert.False(foreignContext.Safety.HasRecentPain);
    }

    [Fact]
    public async Task Coach_base_chat_validates_persists_and_returns_the_complete_ordered_conversation()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var before = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        Assert.NotNull(before);

        var blank = await client.PostAsJsonAsync("/api/v1/coach/messages", new SendCoachMessageRequest("   "));
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        var blankError = await blank.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", blankError!.Code);

        var tooLong = await client.PostAsJsonAsync("/api/v1/coach/messages", new SendCoachMessageRequest(new string('a', 2001)));
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        var tooLongError = await tooLong.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", tooLongError!.Code);

        var posted = await client.PostAsJsonAsync("/api/v1/coach/messages", new SendCoachMessageRequest("Quero trocar um exercício"));
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var returned = await posted.Content.ReadFromJsonAsync<CoachConversationDto>();
        Assert.NotNull(returned);
        Assert.Equal(before!.Id, returned!.Id);
        Assert.Equal(before.Messages.Count + 2, returned.Messages.Count);
        Assert.Equal("Quero trocar um exercício", returned.Messages[^2].Content);
        Assert.Equal("User", returned.Messages[^2].Role);
        Assert.Equal("Assistant", returned.Messages[^1].Role);
        var metadata = JsonSerializer.Deserialize<CoachMessageMetadata>(returned.Messages[^1].MetadataJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(metadata);
        Assert.False(metadata!.RequiresConfirmation);
        Assert.True(metadata.RequiresUserInput);
        Assert.Equal("EXERCISE_SELECTION_REQUIRED", metadata.ReasonCode);
        Assert.True(returned.Messages.SequenceEqual(returned.Messages.OrderBy(message => message.CreatedAt).ThenBy(message => message.Id)));

        var reloaded = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        Assert.NotNull(reloaded);
        Assert.Equal(returned.Messages.Select(message => message.Id), reloaded!.Messages.Select(message => message.Id));
    }

    [Fact]
    public async Task Exercise_substitution_tool_persists_only_an_approved_confirmation_required_proposal()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var today = await client.GetFromJsonAsync<TrainingTodayResponse>("/api/v1/training/today");
        Assert.NotNull(today);
        var current = today!.Exercises[0];
        var replacementId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();
        Guid originalExerciseId;
        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            originalExerciseId = (await db.WorkoutSessionExercises.SingleAsync(exercise => exercise.Id == current.Id)).ExerciseId;
            db.Exercises.AddRange(
                new Exercise { Id = replacementId, Name = "Variação aprovada", PrimaryMuscleGroup = current.PrimaryMuscleGroup },
                new Exercise { Id = invalidId, Name = "Variação inválida", PrimaryMuscleGroup = "Grupo diferente" });
            await db.SaveChangesAsync();
        }

        var proposed = await client.PostAsJsonAsync(
            $"/api/v1/training/sessions/{today.Id}/exercises/{current.Id}/substitution-proposals",
            new CreateExerciseSubstitutionProposalRequest(replacementId));
        Assert.Equal(HttpStatusCode.Created, proposed.StatusCode);
        var action = await proposed.Content.ReadFromJsonAsync<CoachActionDto>();
        Assert.NotNull(action);
        Assert.Equal("ExerciseSubstitution", action!.Type);
        Assert.Equal("Proposed", action.Status);
        Assert.Equal("Yellow", action.SafetyLevel);
        using (var payload = JsonDocument.Parse(action.PayloadJson))
        {
            Assert.Equal(replacementId, payload.RootElement.GetProperty("ReplacementExerciseId").GetGuid());
            Assert.Equal("SAME_PRIMARY_MUSCLE_GROUP", payload.RootElement.GetProperty("ReasonCode").GetString());
        }
        var conversation = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        Assert.NotNull(conversation);
        var actionMessage = Assert.Single(conversation!.Messages, message => message.Kind == "ActionProposal");
        using (var metadata = JsonDocument.Parse(actionMessage.MetadataJson!))
        {
            Assert.Equal(action.Id, metadata.RootElement.GetProperty("actionId").GetGuid());
            Assert.True(metadata.RootElement.GetProperty("requiresConfirmation").GetBoolean());
        }

        var invalid = await client.PostAsJsonAsync(
            $"/api/v1/training/sessions/{today.Id}/exercises/{current.Id}/substitution-proposals",
            new CreateExerciseSubstitutionProposalRequest(invalidId));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var invalidError = await invalid.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_EXERCISE_SUBSTITUTION", invalidError!.Code);

        await using var verificationScope = isolatedFactory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<SvrDbContext>();
        var unchanged = await verificationDb.WorkoutSessionExercises.SingleAsync(exercise => exercise.Id == current.Id);
        Assert.Equal(originalExerciseId, unchanged.ExerciseId);
        Assert.Equal(1, await verificationDb.CoachActions.CountAsync(item => item.MemberId == DemoIds.MemberId && item.Status == "Proposed"));
    }

    [Fact]
    public async Task Coach_action_confirmation_is_idempotent_and_revalidates_safety_before_changing_the_workout()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var today = await client.GetFromJsonAsync<TrainingTodayResponse>("/api/v1/training/today");
        Assert.NotNull(today);
        var current = today!.Exercises[0];
        var replacementId = Guid.NewGuid();
        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            db.Exercises.Add(new Exercise { Id = replacementId, Name = "Alternativa segura", PrimaryMuscleGroup = current.PrimaryMuscleGroup });
            await db.SaveChangesAsync();
        }

        var proposalResponse = await client.PostAsJsonAsync(
            $"/api/v1/training/sessions/{today.Id}/exercises/{current.Id}/substitution-proposals",
            new CreateExerciseSubstitutionProposalRequest(replacementId));
        var action = await proposalResponse.Content.ReadFromJsonAsync<CoachActionDto>();
        Assert.Equal(HttpStatusCode.Created, proposalResponse.StatusCode);
        Assert.NotNull(action);

        var confirmed = await client.PostAsync($"/api/v1/coach/actions/{action!.Id}/confirm", null);
        var confirmedResult = await confirmed.Content.ReadFromJsonAsync<ResolveCoachActionDto>();
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.Equal("Confirmed", confirmedResult!.Status);

        var repeatedConfirmation = await client.PostAsync($"/api/v1/coach/actions/{action.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, repeatedConfirmation.StatusCode);
        Assert.Equal("Confirmed", (await repeatedConfirmation.Content.ReadFromJsonAsync<ResolveCoachActionDto>())!.Status);

        await using (var verificationScope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = verificationScope.ServiceProvider.GetRequiredService<SvrDbContext>();
            Assert.Equal(replacementId, (await db.WorkoutSessionExercises.SingleAsync(exercise => exercise.Id == current.Id)).ExerciseId);
        }

        var rejectedActionId = Guid.NewGuid();
        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            db.CoachActions.Add(new CoachAction { Id = rejectedActionId, MemberId = DemoIds.MemberId, Type = "ExerciseSubstitution", Status = "Proposed", SafetyLevel = "Yellow", PayloadJson = "{}", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var rejected = await client.PostAsync($"/api/v1/coach/actions/{rejectedActionId}/reject", null);
        var repeatedRejection = await client.PostAsync($"/api/v1/coach/actions/{rejectedActionId}/reject", null);
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeatedRejection.StatusCode);
        Assert.Equal("Rejected", (await repeatedRejection.Content.ReadFromJsonAsync<ResolveCoachActionDto>())!.Status);
    }

    [Fact]
    public async Task Fatigue_message_persists_confirmation_required_rest_and_reschedule_options_without_automatic_change()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);
        var today = await client.GetFromJsonAsync<TrainingTodayResponse>("/api/v1/training/today");
        Assert.NotNull(today);

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            db.WorkoutSessions.RemoveRange(await db.WorkoutSessions.Where(session => session.MemberId == DemoIds.MemberId && session.ScheduledFor == today!.ScheduledFor.AddDays(1)).ToListAsync());
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/coach/messages", new SendCoachMessageRequest("Estou muito cansado hoje"));
        var conversation = await response.Content.ReadFromJsonAsync<CoachConversationDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(conversation);
        Assert.Contains(conversation!.Messages, message => message.MetadataJson?.Contains("FATIGUE_NO_APPROVED_ADJUSTMENT") == true);

        var actions = await client.GetFromJsonAsync<List<CoachActionDto>>("/api/v1/coach/actions");
        Assert.NotNull(actions);
        Assert.Contains(actions!, action => action.Type == "WorkoutRest" && action.Status == "Proposed");
        var reschedule = Assert.Single(actions!, action => action.Type == "WorkoutReschedule");

        var confirmed = await client.PostAsync($"/api/v1/coach/actions/{reschedule.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        await using var verificationScope = isolatedFactory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<SvrDbContext>();
        Assert.Equal(today.ScheduledFor.AddDays(1), (await verificationDb.WorkoutSessions.SingleAsync(session => session.Id == today.Id)).ScheduledFor);
    }

    [Fact]
    public async Task Demo_reset_is_available_only_for_an_isolated_demo_database()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var reset = await client.PostAsync("/api/v1/demo/reset", null);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.NotNull(await client.GetFromJsonAsync<TrainingTodayResponse>("/api/v1/training/today"));

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            var user = new AuthUser { Id = Guid.NewGuid(), Email = "outside-demo@example.test", CreatedAt = DateTimeOffset.UtcNow };
            db.Members.Add(new Member { Id = Guid.NewGuid(), AuthUser = user, FirstName = "Outro", LastName = "Membro", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var blocked = await client.PostAsync("/api/v1/demo/reset", null);
        var error = await blocked.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("DEMO_RESET_NOT_SAFE", error!.Code);
    }

    [Fact]
    public async Task Pain_reporting_validates_required_fields_and_persists_a_member_scoped_report()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var invalidIntensity = await client.PostAsJsonAsync("/api/v1/health/pain-reports", new PainReportRequest("Joelho", "Direito", 11, "Agachamento"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidIntensity.StatusCode);
        var intensityError = await invalidIntensity.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", intensityError!.Code);

        var invalidSide = await client.PostAsJsonAsync("/api/v1/health/pain-reports", new PainReportRequest("Joelho", "Centro", 5, "Agachamento"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidSide.StatusCode);

        var missingContext = await client.PostAsJsonAsync("/api/v1/health/pain-reports", new PainReportRequest("Joelho", "Direito", 5, " "));
        Assert.Equal(HttpStatusCode.BadRequest, missingContext.StatusCode);

        var created = await client.PostAsJsonAsync("/api/v1/health/pain-reports", new PainReportRequest("  Joelho  ", "  Direito ", 5, "  Durante o agachamento  "));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var response = await created.Content.ReadFromJsonAsync<PainReportDto>();
        Assert.NotNull(response);
        Assert.Equal("Yellow", response!.SafetyLevel);
        Assert.Equal("PAIN_MODERATE_INTENSITY", response.ReasonCode);

        var conversation = await client.GetFromJsonAsync<CoachConversationDto>("/api/v1/coach/conversation");
        var coachPainMessage = Assert.Single(conversation!.Messages, message => message.MetadataJson?.Contains("PAIN_MODERATE_INTENSITY") == true);
        Assert.Equal("Assistant", coachPainMessage.Role);
        Assert.Equal("Text", coachPainMessage.Kind);
        Assert.Contains("Não faremos alterações automáticas", coachPainMessage.Content);
        var metadata = JsonSerializer.Deserialize<CoachMessageMetadata>(coachPainMessage.MetadataJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("PAIN_MODERATE_INTENSITY", metadata!.ReasonCode);
        Assert.False(metadata.RequiresConfirmation);

        await using var scope = isolatedFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
        var report = await db.PainReports.SingleAsync(item => item.Id == response.Id);
        Assert.Equal(DemoIds.MemberId, report.MemberId);
        Assert.Equal("Joelho", report.Area);
        Assert.Equal("Direito", report.Side);
        Assert.Equal(5, report.Intensity);
        Assert.Equal("Durante o agachamento", report.Context);
        Assert.Equal("PAIN_MODERATE_INTENSITY", report.ReasonCode);
        Assert.Empty(await db.CoachActions.Where(item => item.MemberId == DemoIds.MemberId).ToListAsync());
    }

    [Fact]
    public async Task Nutrition_today_returns_the_active_plan_in_meal_order_with_daily_completion_status()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            var nutrition = await db.NutritionPlans.Include(x => x.Meals)
                .SingleAsync(x => x.Plan.MemberId == DemoIds.MemberId);
            var dinner = nutrition.Meals.Single(x => x.Name == "Jantar");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var log = await db.DailyLogs.SingleOrDefaultAsync(x => x.MemberId == DemoIds.MemberId && x.MealTemplateId == dinner.Id && x.Date == today);
            if (log is null)
            {
                db.DailyLogs.Add(new DailyLog { Id = Guid.NewGuid(), MemberId = DemoIds.MemberId, MealTemplateId = dinner.Id, Date = today, Completed = true });
            }
            else log.Completed = true;
            await db.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<NutritionTodayDto>("/api/v1/nutrition/today");

        Assert.NotNull(response);
        Assert.Equal(2600, response!.CaloriesTarget);
        Assert.Equal(180, response.ProteinTarget);
        Assert.Equal(300, response.CarbsTarget);
        Assert.Equal(70, response.FatTarget);
        Assert.Equal(["Café da manhã", "Lanche da manhã", "Almoço", "Pré-treino", "Jantar", "Ceia"], response.Meals.Select(x => x.Name));
        Assert.True(response.Meals.Single(x => x.Name == "Café da manhã").Completed);
        Assert.True(response.Meals.Single(x => x.Name == "Jantar").Completed);
        Assert.All(response.Meals.SelectMany(x => x.Foods), food =>
        {
            Assert.True(food.QuantityGrams > 0);
            Assert.True(food.Calories > 0);
            Assert.True(food.Protein >= 0);
            Assert.True(food.Carbs >= 0);
            Assert.True(food.Fat >= 0);
        });
    }

    [Fact]
    public async Task Nutrition_today_returns_the_documented_error_when_the_member_has_no_active_plan()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        await using (var scope = isolatedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
            var plan = await db.Plans.SingleAsync(x => x.MemberId == DemoIds.MemberId && x.Status == "Active");
            plan.Status = "Inactive";
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/nutrition/today");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NO_ACTIVE_PLAN", error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
    }

    [Fact]
    public async Task Demo_user_can_substitute_a_food_with_a_no_content_response()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var nutrition = await client.GetFromJsonAsync<NutritionTodayDto>("/api/v1/nutrition/today");
        var lunch = Assert.Single(nutrition!.Meals, meal => meal.Name == "Almoço");
        var chicken = Assert.Single(lunch.Foods, food => food.Name == "Frango grelhado");
        var alternatives = await client.GetFromJsonAsync<List<FoodAlternativeDto>>($"/api/v1/nutrition/meals/{lunch.Id}/foods/{chicken.FoodId}/alternatives");
        var replacement = Assert.Single(alternatives!, food => food.Name == "Patinho moído");

        var substitute = await client.PostAsJsonAsync(
            $"/api/v1/nutrition/meals/{lunch.Id}/foods/{chicken.FoodId}/substitute",
            new SubstituteFoodRequest(replacement.FoodId));

        Assert.Equal(HttpStatusCode.NoContent, substitute.StatusCode);
        Assert.Empty(await substitute.Content.ReadAsStringAsync());

        var updatedMeal = await client.GetFromJsonAsync<MealDto>($"/api/v1/nutrition/meals/{lunch.Id}");
        Assert.Contains(updatedMeal!.Foods, food => food.FoodId == replacement.FoodId && food.Name == replacement.Name);
        Assert.DoesNotContain(updatedMeal.Foods, food => food.FoodId == chicken.FoodId);
    }

    [Fact]
    public async Task Food_alternatives_are_scoped_to_the_member_meal_and_preserve_calories()
    {
        using var isolatedFactory = new SvrApiFactory();
        isolatedFactory.Seed();
        using var client = isolatedFactory.CreateClient();
        await AuthenticateAsync(client);

        var nutrition = await client.GetFromJsonAsync<NutritionTodayDto>("/api/v1/nutrition/today");
        var lunch = Assert.Single(nutrition!.Meals, meal => meal.Name == "Almoço");
        var chicken = Assert.Single(lunch.Foods, food => food.Name == "Frango grelhado");
        var alternativesResponse = await client.GetAsync($"/api/v1/nutrition/meals/{lunch.Id}/foods/{chicken.FoodId}/alternatives");
        var alternatives = await alternativesResponse.Content.ReadFromJsonAsync<List<FoodAlternativeDto>>();

        Assert.Equal(HttpStatusCode.OK, alternativesResponse.StatusCode);
        var replacement = Assert.Single(alternatives!, food => food.Name == "Patinho moído");
        Assert.Equal(136m, replacement.SuggestedQuantityGrams);
        Assert.Equal("NUTRITION_CALORIE_EQUIVALENT", replacement.ReasonCode);
        Assert.Equal(297m, chicken.Calories);
        Assert.InRange(replacement.SuggestedQuantityGrams * 219m / 100m, 296m, 298m);

        await using var scope = isolatedFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
        var replacementFood = await db.Foods.SingleAsync(food => food.Id == replacement.FoodId);
        var chickenFood = await db.Foods.SingleAsync(food => food.Id == chicken.FoodId);
        Assert.Equal(chickenFood.Category, replacementFood.Category);

        var foreignUser = new AuthUser { Id = Guid.NewGuid(), Email = "other-member@svr.method", CreatedAt = DateTimeOffset.UtcNow };
        var foreignMember = new Member { Id = Guid.NewGuid(), AuthUserId = foreignUser.Id, FirstName = "Outra", LastName = "Pessoa", CreatedAt = DateTimeOffset.UtcNow };
        var methodology = await db.MethodologyVersions.SingleAsync();
        var foreignPlan = new Plan { Id = Guid.NewGuid(), MemberId = foreignMember.Id, MethodologyVersionId = methodology.Id, Name = "Outro plano", Status = "Active", StartsOn = DateOnly.FromDateTime(DateTime.UtcNow), CreatedAt = DateTimeOffset.UtcNow };
        var foreignNutrition = new NutritionPlan { Id = Guid.NewGuid(), PlanId = foreignPlan.Id, CaloriesTarget = 2000, ProteinGramsTarget = 150, CarbsGramsTarget = 200, FatGramsTarget = 60 };
        var foreignMealTemplate = new MealTemplate { Id = Guid.NewGuid(), NutritionPlanId = foreignNutrition.Id, Name = "Almoço", Sequence = 1 };
        foreignMealTemplate.Foods.Add(new MealTemplateFood { Id = Guid.NewGuid(), FoodId = chicken.FoodId, QuantityGrams = 100 });
        db.AddRange(foreignUser, foreignMember, foreignPlan, foreignNutrition, foreignMealTemplate);
        await db.SaveChangesAsync();

        var foreignMeal = await client.GetAsync($"/api/v1/nutrition/meals/{foreignMealTemplate.Id}/foods/{chicken.FoodId}/alternatives");
        Assert.Equal(HttpStatusCode.NotFound, foreignMeal.StatusCode);

        var crossCategory = await db.Foods.SingleAsync(food => food.Name == "Arroz branco cozido");
        var invalid = await client.PostAsJsonAsync(
            $"/api/v1/nutrition/meals/{lunch.Id}/foods/{chicken.FoodId}/substitute",
            new SubstituteFoodRequest(crossCategory.Id));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var error = await invalid.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_FOOD_SUBSTITUTION", error!.Code);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/dev-login", new { });
        var credentials = await login.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(credentials);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credentials!.TokenType, credentials.AccessToken);
    }

    private static async Task<MemberDto> AuthenticateNewCompletedMemberAsync(HttpClient client, string? email = null)
    {
        email ??= $"plan-member-{Guid.NewGuid():N}@example.test";
        var login = await client.PostAsJsonAsync("/api/v1/auth/dev-login", new DevLoginRequest(email));
        var credentials = (await login.Content.ReadFromJsonAsync<DevLoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credentials.TokenType, credentials.AccessToken);
        var draft = new SaveOnboardingProfileRequest("Rafaela", "Silva", "Ganhar massa", "Iniciante", 4, 60,
            "Academia", "Halteres, máquinas e elásticos", 165, 64.5m, "Nenhuma informada", "Nenhuma informada",
            "Sem dor atual", "Refeições simples", "Nenhuma informada", 7);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/onboarding/profile", draft)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/onboarding/complete", null)).StatusCode);
        return credentials.Member;
    }
}

public sealed class SvrApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"svr-api-tests-{Guid.NewGuid()}";

    public void Seed()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SvrDbContext>();
        db.Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DemoData:SeedOnStartup"] = "true",
            ["DemoData:AllowReset"] = "true"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SvrDbContext>>();
            services.RemoveAll<SvrDbContext>();
            services.AddDbContext<SvrDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
