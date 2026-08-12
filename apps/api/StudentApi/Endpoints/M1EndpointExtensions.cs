using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.StudentApi.Contracts;
using PersonalUltra.Application.Coach;
using PersonalUltra.Application.Nutrition;
using PersonalUltra.Application.Safety;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;

namespace PersonalUltra.StudentApi.Endpoints;

public static class M1EndpointExtensions
{
    private const int MaxCoachMessageLength = 2000;
    private static readonly HashSet<string> ValidPainSides = new(StringComparer.Ordinal) { "Esquerdo", "Direito", "Bilateral", "Não informado" };

    public static void MapM1Api(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireAuthorization();
        api.MapGet("/progress/summary", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var id = MemberId(user); var weights = await db.WeightEntries.Where(x => x.MemberId == id).OrderBy(x => x.RecordedAt).ToListAsync(ct);
            var sessions = await db.WorkoutSessions.CountAsync(x => x.MemberId == id && x.Status == "Completed", ct);
            var scheduledSessions = await db.WorkoutSessions.CountAsync(x => x.MemberId == id && x.ScheduledFor <= DateOnly.FromDateTime(DateTime.UtcNow), ct);
            var planStart = await db.Plans.Where(x => x.MemberId == id && x.Status == "Active").Select(x => x.StartsOn).SingleOrDefaultAsync(ct);
            var performances = await db.SetPerformances.Where(x => x.WorkoutSessionExercise.WorkoutSession.MemberId == id)
                .Select(x => new { x.WorkoutSessionExercise.Exercise.Name, x.WeightKg, x.CompletedAt }).ToListAsync(ct);
            var strength = performances.GroupBy(x => x.Name).Select(group =>
            {
                var ordered = group.OrderBy(x => x.CompletedAt).ToList();
                var first = ordered.First(); var latest = ordered.Last();
                var changePercent = first.WeightKg == 0 ? 0 : Math.Round((latest.WeightKg - first.WeightKg) * 100 / first.WeightKg, 1);
                return new StrengthProgressDto(group.Key, latest.WeightKg, changePercent);
            }).OrderByDescending(x => x.ChangePercent).ThenByDescending(x => x.CurrentLoadKg).FirstOrDefault();
            var current = weights.LastOrDefault()?.WeightKg ?? 0; var change = weights.Count > 1 ? current - weights.First().WeightKg : 0;
            var consistency = scheduledSessions == 0 ? 0 : (int)Math.Round(sessions * 100d / scheduledSessions, MidpointRounding.AwayFromZero);
            var daysOnMethod = planStart == default ? 0 : Math.Max(1, DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - planStart.DayNumber + 1);
            return Results.Ok(new ProgressSummaryDto(current, change, sessions, consistency, daysOnMethod, strength, "Força e consistência em evolução."));
        });
        api.MapGet("/progress/weight", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await db.WeightEntries.Where(x => x.MemberId == MemberId(user)).OrderBy(x => x.RecordedAt).ToListAsync(ct)).Select(x => new WeightDto(x.Id, x.WeightKg, x.RecordedAt))));
        api.MapPost("/progress/weight", async (CreateWeightRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken ct) =>
        {
            if (request.WeightKg is < 25 or > 400) return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "Peso inválido." });
            var entry = new WeightEntry { Id = Guid.NewGuid(), MemberId = MemberId(user), WeightKg = request.WeightKg, RecordedAt = request.RecordedAt ?? clock.GetUtcNow() }; db.WeightEntries.Add(entry); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/progress/weight/{entry.Id}", new WeightDto(entry.Id, entry.WeightKg, entry.RecordedAt));
        });
        api.MapGet("/nutrition/today", async (PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken ct) =>
        {
            var memberId = MemberId(user);
            var plan = await db.Plans.Where(x => x.MemberId == memberId && x.Status == "Active").Select(x => x.Id).SingleOrDefaultAsync(ct);
            if (plan == Guid.Empty)
                return ApiEndpointExtensions.ApiError("NO_ACTIVE_PLAN", "Não há um plano ativo para este membro.", StatusCodes.Status404NotFound);
            var nutrition = await db.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods).ThenInclude(x => x.Food).SingleOrDefaultAsync(x => x.PlanId == plan, ct);
            if (nutrition is null)
                return ApiEndpointExtensions.ApiError("NUTRITION_PLAN_NOT_READY", "O plano alimentar ainda não foi preparado.", StatusCodes.Status409Conflict);
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var logs = await db.DailyLogs.Where(x => x.MemberId == memberId && x.Date == today).ToListAsync(ct);
            return Results.Ok(new NutritionTodayDto(nutrition.CaloriesTarget, nutrition.ProteinGramsTarget, nutrition.CarbsGramsTarget, nutrition.FatGramsTarget, nutrition.Meals.OrderBy(x => x.Sequence).Select(m => Meal(m, logs.Any(l => l.MealTemplateId == m.Id && l.Completed))).ToList()));
        });
        api.MapGet("/nutrition/meals/{mealId:guid}", async (Guid mealId, PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var memberId = MemberId(user); var meal = await MealForMember(db, mealId, memberId, ct); if (meal is null) return Results.NotFound();
            var completed = await db.DailyLogs.AnyAsync(x => x.MemberId == memberId && x.MealTemplateId == mealId && x.Date == DateOnly.FromDateTime(DateTime.UtcNow) && x.Completed, ct);
            return Results.Ok(Meal(meal, completed));
        });
        api.MapPost("/nutrition/meals/{mealId:guid}/complete", async (Guid mealId, PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        { var id = MemberId(user); if (await MealForMember(db, mealId, id, ct) is null) return Results.NotFound(); var date = DateOnly.FromDateTime(DateTime.UtcNow); var log = await db.DailyLogs.SingleOrDefaultAsync(x => x.MemberId == id && x.Date == date && x.MealTemplateId == mealId, ct); if (log is null) db.DailyLogs.Add(new DailyLog { Id = Guid.NewGuid(), MemberId = id, Date = date, MealTemplateId = mealId, Completed = true }); else log.Completed = true; await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapGet("/nutrition/meals/{mealId:guid}/foods/{foodId:guid}/alternatives", async (Guid mealId, Guid foodId, PersonalUltraDbContext db, ClaimsPrincipal user, FoodAlternativesEngine alternativesEngine, CancellationToken ct) =>
        {
            var meal = await MealForMember(db, mealId, MemberId(user), ct);
            var original = meal?.Foods.SingleOrDefault(x => x.FoodId == foodId);
            if (original is null) return Results.NotFound();

            var catalog = await db.Foods.AsNoTracking().ToListAsync(ct);
            var alternatives = alternativesEngine.FindApprovedAlternatives(original, catalog)
                .Select(candidate => new FoodAlternativeDto(candidate.FoodId, candidate.Name, candidate.SuggestedQuantityGrams, candidate.ReasonCode));
            return Results.Ok(alternatives);
        });
        api.MapPost("/nutrition/meals/{mealId:guid}/foods/{foodId:guid}/substitute", async (Guid mealId, Guid foodId, SubstituteFoodRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, FoodAlternativesEngine alternativesEngine, CancellationToken ct) =>
        {
            var meal = await MealForMember(db, mealId, MemberId(user), ct);
            var item = meal?.Foods.SingleOrDefault(x => x.FoodId == foodId);
            var replacement = request.FoodId == Guid.Empty ? null : await db.Foods.FindAsync([request.FoodId], ct);
            if (item is null || replacement is null || !alternativesEngine.IsApprovedAlternative(item.Food, replacement))
                return ApiEndpointExtensions.ApiError("INVALID_FOOD_SUBSTITUTION", "A substituição precisa usar um alimento aprovado da mesma categoria.", StatusCodes.Status400BadRequest);

            item.QuantityGrams = alternativesEngine.CalculateCalorieEquivalentQuantity(item.QuantityGrams, item.Food, replacement);
            item.FoodId = replacement.Id;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        api.MapPost("/health/pain-reports", async (PainReportRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, PainSafetyEngine painSafetyEngine, CoachOutputValidator outputValidator, CancellationToken ct) =>
        {
            var area = request.Area?.Trim();
            var side = request.Side?.Trim();
            var context = request.Context?.Trim();
            if (string.IsNullOrWhiteSpace(area) || area.Length > 100 ||
                string.IsNullOrWhiteSpace(side) || !ValidPainSides.Contains(side) ||
                request.Intensity is < 0 or > 10 ||
                string.IsNullOrWhiteSpace(context) || context.Length > 500)
            {
                return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "Informe região, lado, intensidade de 0 a 10 e contexto da dor.", StatusCodes.Status400BadRequest);
            }

            var decision = painSafetyEngine.Evaluate(request.Intensity, context);
            var report = new PainReport { Id = Guid.NewGuid(), MemberId = MemberId(user), Area = area, Side = side, Intensity = request.Intensity, Context = context, SafetyLevel = decision.SafetyLevel, ReasonCode = decision.ReasonCode, ReportedAt = clock.GetUtcNow() };
            db.PainReports.Add(report);
            var conversation = await GetOrCreateCoachConversationAsync(db, report.MemberId, clock, ct);
            var coachReply = PainCoachReply(decision);
            var structuredReply = outputValidator.Validate(coachReply);
            db.CoachMessages.Add(new CoachMessage
            {
                Id = Guid.NewGuid(),
                Conversation = conversation,
                Role = "Assistant",
                Kind = structuredReply.Kind,
                Content = structuredReply.Content,
                MetadataJson = structuredReply.MetadataJson,
                CreatedAt = clock.GetUtcNow(),
            });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new PainReportDto(report.Id, decision.SafetyLevel, decision.ReasonCode, decision.Message, decision.RequiresConfirmation));
        });
        api.MapGet("/coach/conversation", async (PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken ct) =>
        {
            var conversation = await GetOrCreateCoachConversationAsync(db, MemberId(user), clock, ct);
            return Results.Ok(ToCoachConversation(conversation));
        });
        api.MapPost("/coach/messages", async (SendCoachMessageRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CoachContextBuilder contextBuilder, ICoachResponder responder, CoachOutputValidator outputValidator, CancellationToken ct) =>
        {
            var content = request.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "Escreva uma mensagem para o Coach.", StatusCodes.Status400BadRequest);
            if (content.Length > MaxCoachMessageLength)
                return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", $"A mensagem pode ter no máximo {MaxCoachMessageLength} caracteres.", StatusCodes.Status400BadRequest);

            var memberId = MemberId(user);
            var conversation = await GetOrCreateCoachConversationAsync(db, memberId, clock, ct);
            var inputAt = clock.GetUtcNow();
            var input = new CoachMessage { Id = Guid.NewGuid(), Conversation = conversation, Role = "User", Kind = "Text", Content = content, CreatedAt = inputAt };
            CoachReply reply;
            try
            {
                reply = await responder.ReplyAsync(input.Content, await contextBuilder.BuildAsync(memberId, ct), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return ApiEndpointExtensions.ApiError("COACH_UNAVAILABLE", "O SVR Coach está indisponível no momento. Tente novamente em instantes.", StatusCodes.Status503ServiceUnavailable);
            }

            ValidatedCoachReply structuredReply;
            try
            {
                structuredReply = outputValidator.Validate(reply);
            }
            catch (CoachOutputValidationException)
            {
                return ApiEndpointExtensions.ApiError("COACH_OUTPUT_INVALID", "O SVR Coach não retornou uma resposta válida. Tente novamente em instantes.", StatusCodes.Status503ServiceUnavailable);
            }

            var outputAt = clock.GetUtcNow();
            if (outputAt <= inputAt) outputAt = inputAt.AddTicks(1);
            var output = new CoachMessage { Id = Guid.NewGuid(), Conversation = conversation, Role = "Assistant", Kind = structuredReply.Kind, Content = structuredReply.Content, MetadataJson = structuredReply.MetadataJson, CreatedAt = outputAt };
            db.CoachMessages.AddRange(input, output);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToCoachConversation(conversation));
        });
        api.MapPost("/demo/reset", async (IHostEnvironment environment, IConfiguration configuration, DemoDataSeeder seeder, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DemoData:AllowReset")) return Results.NotFound();
            if (!await seeder.IsSafeToResetAsync(ct))
                return ApiEndpointExtensions.ApiError("DEMO_RESET_NOT_SAFE", "O reset só é permitido em uma base isolada de demonstração.", StatusCodes.Status409Conflict);
            await seeder.ResetAsync(ct); return Results.NoContent();
        });
        api.MapPost("/demo/member-reset", async (IHostEnvironment environment, IConfiguration configuration, MemberDemoResetService resetService, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DemoData:AllowReset")) return Results.NotFound();

            var result = await resetService.ResetAsync(MemberId(user), ct);
            return result switch
            {
                MemberDemoResetResult.Reset => Results.NoContent(),
                MemberDemoResetResult.BaseDemoAccount => ApiEndpointExtensions.ApiError("DEMO_BASE_ACCOUNT_PROTECTED", "A conta base da demonstração não pode ser apagada.", StatusCodes.Status409Conflict),
                _ => Results.NotFound(),
            };
        });
    }

    private static Guid MemberId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static async Task<Conversation> GetOrCreateCoachConversationAsync(PersonalUltraDbContext db, Guid memberId, TimeProvider clock, CancellationToken cancellationToken)
    {
        var conversation = await db.Conversations.Include(x => x.Messages).SingleOrDefaultAsync(x => x.MemberId == memberId, cancellationToken);
        if (conversation is not null) return conversation;

        conversation = new Conversation { Id = Guid.NewGuid(), MemberId = memberId, CreatedAt = clock.GetUtcNow() };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return conversation;
    }
    private static async Task<MealTemplate?> MealForMember(PersonalUltraDbContext db, Guid mealId, Guid memberId, CancellationToken ct) => await db.MealTemplates.Include(x => x.Foods).ThenInclude(x => x.Food).Include(x => x.NutritionPlan).ThenInclude(x => x.Plan).SingleOrDefaultAsync(x => x.Id == mealId && x.NutritionPlan.Plan.MemberId == memberId, ct);
    private static MealDto Meal(MealTemplate meal, bool completed) => new(meal.Id, meal.Name, completed, meal.Foods.Select(x => new MealFoodDto(x.Id, x.FoodId, x.Food.Name, x.QuantityGrams, Math.Round(x.QuantityGrams * x.Food.CaloriesPer100g / 100, 0), Math.Round(x.QuantityGrams * x.Food.ProteinPer100g / 100, 1), Math.Round(x.QuantityGrams * x.Food.CarbsPer100g / 100, 1), Math.Round(x.QuantityGrams * x.Food.FatPer100g / 100, 1))).ToList());
    private static CoachMessageDto Message(CoachMessage message) => new(message.Id, message.Role, message.Kind, message.Content, message.MetadataJson, message.CreatedAt);
    private static CoachConversationDto ToCoachConversation(Conversation conversation) => new(conversation.Id, conversation.Messages.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(Message).ToList());
    private static CoachReply PainCoachReply(PainSafetyDecision decision)
    {
        var content = decision.SafetyLevel switch
        {
            "Red" => "Recebi seu relato de dor intensa. Não faremos alterações automáticas no seu treino. Interrompa a atividade que provocou dor e procure orientação profissional antes de retomá-la.",
            "Yellow" => "Recebi seu relato de dor moderada. Não faremos alterações automáticas no seu treino. Vamos manter isso em revisão antes de qualquer mudança.",
            _ => "Recebi seu relato de dor. Nenhuma alteração foi feita automaticamente. Observe a resposta nas próximas séries e registre novamente se a dor persistir ou aumentar.",
        };
        return new CoachReply(CoachMessageKinds.Text, content, decision.ReasonCode);
    }
}
