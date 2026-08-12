using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Api.Contracts;
using PersonalUltra.Api.Application.Coach;
using PersonalUltra.Api.Application.Nutrition;
using PersonalUltra.Api.Application.Training;
using PersonalUltra.Api.Application.Safety;
using PersonalUltra.Api.Domain;
using PersonalUltra.Api.Infrastructure;

namespace PersonalUltra.Api.Endpoints;

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
            if (IsFatigueMessage(input.Content))
            {
                await PersistFatigueOptionsAsync(db, conversation, input, memberId, clock, ct);
                return Results.Ok(ToCoachConversation(conversation));
            }
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
        api.MapGet("/training/sessions/{sessionId:guid}/exercises/{exerciseId:guid}/alternatives", async (Guid sessionId, Guid exerciseId, PersonalUltraDbContext db, ClaimsPrincipal user, ExerciseAlternativesEngine alternativesEngine, CancellationToken ct) =>
        {
            var current = await db.WorkoutSessionExercises.Include(x => x.Exercise).Include(x => x.WorkoutSession)
                .SingleOrDefaultAsync(x => x.Id == exerciseId && x.WorkoutSessionId == sessionId && x.WorkoutSession.MemberId == MemberId(user), ct);
            if (current is null) return Results.NotFound();

            var catalog = await db.Exercises.AsNoTracking().ToListAsync(ct);
            var alternatives = alternativesEngine.FindApprovedAlternatives(current.Exercise, catalog)
                .Select(candidate => new ExerciseAlternativeDto(candidate.ExerciseId, candidate.Name, candidate.ReasonCode));
            return Results.Ok(alternatives);
        });
        api.MapPost("/training/sessions/{sessionId:guid}/exercises/{exerciseId:guid}/substitution-proposals", async (Guid sessionId, Guid exerciseId, CreateExerciseSubstitutionProposalRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, ExerciseSubstitutionTool substitutionTool, CancellationToken ct) =>
        {
            var memberId = MemberId(user);
            var current = await db.WorkoutSessionExercises.Include(x => x.Exercise).Include(x => x.WorkoutSession).SingleOrDefaultAsync(x => x.Id == exerciseId && x.WorkoutSessionId == sessionId && x.WorkoutSession.MemberId == memberId, ct);
            var replacement = await db.Exercises.FindAsync([request.ExerciseId], ct);
            var proposal = current is null || replacement is null ? null : substitutionTool.CreateProposal(current, replacement);
            if (proposal is null)
                return ApiEndpointExtensions.ApiError("INVALID_EXERCISE_SUBSTITUTION", "A substituição precisa preservar a musculatura primária do exercício.", StatusCodes.Status400BadRequest);

            var action = new CoachAction
            {
                Id = Guid.NewGuid(),
                MemberId = memberId,
                Type = "ExerciseSubstitution",
                Status = "Proposed",
                SafetyLevel = proposal.SafetyLevel,
                CreatedAt = clock.GetUtcNow(),
                PayloadJson = JsonSerializer.Serialize(new ExerciseSubstitutionPayload(proposal.SessionId, proposal.WorkoutSessionExerciseId, proposal.ReplacementExerciseId, proposal.ReasonCode)),
            };
            var conversation = await GetOrCreateCoachConversationAsync(db, memberId, clock, ct);
            db.CoachActions.Add(action);
            db.CoachMessages.Add(new CoachMessage
            {
                Id = Guid.NewGuid(),
                Conversation = conversation,
                Role = "Assistant",
                Kind = CoachMessageKinds.ActionProposal,
                Content = "Encontrei uma alternativa que preserva a musculatura primária. Revise a proposta antes de aplicá-la ao seu treino.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    reasonCode = proposal.ReasonCode,
                    messageType = CoachMessageKinds.ActionProposal,
                    requiresUserInput = false,
                    requiresConfirmation = proposal.RequiresConfirmation,
                    actionId = action.Id,
                    proposalType = "Substituição de exercício",
                    safetyLevel = proposal.SafetyLevel,
                }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                CreatedAt = action.CreatedAt,
            });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/coach/actions/{action.Id}", Action(action));
        });
        api.MapGet("/coach/actions", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await db.CoachActions.Where(x => x.MemberId == MemberId(user) && x.Status == "Proposed").OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(Action)));
        api.MapPost("/coach/actions/{actionId:guid}/confirm", async (Guid actionId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, ExerciseSubstitutionTool substitutionTool, CancellationToken ct) =>
        {
            var action = await db.CoachActions.SingleOrDefaultAsync(x => x.Id == actionId && x.MemberId == MemberId(user), ct);
            if (action is null) return Results.NotFound();
            if (action.Status == "Confirmed") return Results.Ok(new ResolveCoachActionDto(action.Id, action.Status, "Esta alteração já foi aplicada."));
            if (action.Status != "Proposed") return Results.Conflict(new { code = "ACTION_ALREADY_RESOLVED" });
            if (action.SafetyLevel != ExerciseSubstitutionTool.SafetyLevel)
                return ApiEndpointExtensions.ApiError("SAFETY_ACTION_BLOCKED", "Esta proposta não pode ser aplicada automaticamente.", StatusCodes.Status409Conflict);

            if (action.Type == "ExerciseSubstitution")
            {
                var payload = DeserializeExerciseSubstitutionPayload(action.PayloadJson);
                var target = payload is null ? null : await db.WorkoutSessionExercises.Include(x => x.Exercise).Include(x => x.WorkoutSession)
                    .SingleOrDefaultAsync(x => x.Id == payload.WorkoutSessionExerciseId && x.WorkoutSessionId == payload.SessionId && x.WorkoutSession.MemberId == action.MemberId, ct);
                var replacement = payload is null ? null : await db.Exercises.FindAsync([payload.ReplacementExerciseId], ct);
                if (payload is null || target is null || replacement is null || payload.ReasonCode != ExerciseAlternativesEngine.SamePrimaryMuscleGroupReasonCode || substitutionTool.CreateProposal(target, replacement) is null)
                    return ApiEndpointExtensions.ApiError("SAFETY_ACTION_BLOCKED", "A proposta não é mais segura para ser aplicada.", StatusCodes.Status409Conflict);

                target.ExerciseId = payload.ReplacementExerciseId;
                target.ExerciseSnapshotJson = JsonSerializer.Serialize(new { source = "coach-confirmed-substitution", replacementExerciseId = payload.ReplacementExerciseId });
            }
            else if (action.Type == "WorkoutReschedule")
            {
                var payload = DeserializeWorkoutReschedulePayload(action.PayloadJson);
                var session = payload is null ? null : await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == payload.SessionId && x.MemberId == action.MemberId, ct);
                var hasConflict = payload is not null && await db.WorkoutSessions.AnyAsync(x => x.MemberId == action.MemberId && x.ScheduledFor == payload.TargetDate && x.Id != payload.SessionId, ct);
                if (payload is null || session is null || session.Status != "Planned" || session.ScheduledFor != payload.SourceScheduledFor || payload.TargetDate != payload.SourceScheduledFor.AddDays(1) || payload.ReasonCode != "FATIGUE_RESCHEDULE_NEXT_DAY" || hasConflict)
                    return ApiEndpointExtensions.ApiError("SAFETY_ACTION_BLOCKED", "O reagendamento não é mais seguro para ser aplicado.", StatusCodes.Status409Conflict);
                session.ScheduledFor = payload.TargetDate;
            }
            else if (action.Type == "WorkoutRest")
            {
                var payload = DeserializeWorkoutRestPayload(action.PayloadJson);
                var session = payload is null ? null : await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == payload.SessionId && x.MemberId == action.MemberId, ct);
                if (payload is null || session is null || session.Status != "Planned" || payload.ReasonCode != "FATIGUE_REST_DAY")
                    return ApiEndpointExtensions.ApiError("SAFETY_ACTION_BLOCKED", "O descanso não é mais seguro para ser registrado.", StatusCodes.Status409Conflict);
                session.Status = "Skipped";
            }
            else return ApiEndpointExtensions.ApiError("SAFETY_ACTION_BLOCKED", "Esta proposta não pode ser aplicada automaticamente.", StatusCodes.Status409Conflict);
            action.Status = "Confirmed"; action.ResolvedAt = clock.GetUtcNow(); await db.SaveChangesAsync(ct);
            return Results.Ok(new ResolveCoachActionDto(action.Id, action.Status, "Alteração aplicada após sua confirmação."));
        });
        api.MapPost("/coach/actions/{actionId:guid}/reject", async (Guid actionId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken ct) =>
        {
            var action = await db.CoachActions.SingleOrDefaultAsync(x => x.Id == actionId && x.MemberId == MemberId(user), ct);
            if (action is null) return Results.NotFound();
            if (action.Status == "Rejected") return Results.Ok(new ResolveCoachActionDto(action.Id, action.Status, "Esta proposta já foi descartada."));
            if (action.Status != "Proposed") return Results.Conflict(new { code = "ACTION_ALREADY_RESOLVED" });
            action.Status = "Rejected"; action.ResolvedAt = clock.GetUtcNow(); await db.SaveChangesAsync(ct);
            return Results.Ok(new ResolveCoachActionDto(action.Id, action.Status, "Proposta descartada."));
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
    private static bool IsFatigueMessage(string content) => content.Contains("cansad", StringComparison.OrdinalIgnoreCase) || content.Contains("fadig", StringComparison.OrdinalIgnoreCase);
    private static async Task PersistFatigueOptionsAsync(PersonalUltraDbContext db, Conversation conversation, CoachMessage input, Guid memberId, TimeProvider clock, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var session = await db.WorkoutSessions.SingleOrDefaultAsync(x => x.MemberId == memberId && x.ScheduledFor == today && x.Status == "Planned", ct);
        var outputs = new List<CoachMessage>
        {
            new()
            {
                Id = Guid.NewGuid(), Conversation = conversation, Role = "Assistant", Kind = CoachMessageKinds.Text,
                Content = "Entendi a fadiga. Não há uma regra aprovada nesta versão para ajustar carga ou volume. Você pode remarcar o treino ou registrar um dia de descanso; nada será alterado sem sua confirmação.",
                MetadataJson = JsonSerializer.Serialize(new CoachMessageMetadata("FATIGUE_NO_APPROVED_ADJUSTMENT", CoachMessageKinds.Text, false, false), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                CreatedAt = input.CreatedAt.AddTicks(1),
            },
        };
        if (session is not null)
        {
            var restAction = new CoachAction
            {
                Id = Guid.NewGuid(), MemberId = memberId, Type = "WorkoutRest", Status = "Proposed", SafetyLevel = ExerciseSubstitutionTool.SafetyLevel, CreatedAt = input.CreatedAt.AddTicks(2),
                PayloadJson = JsonSerializer.Serialize(new WorkoutRestPayload(session.Id, "FATIGUE_REST_DAY")),
            };
            db.CoachActions.Add(restAction);
            outputs.Add(FatigueProposalMessage(conversation, restAction, "Registrar descanso", "FATIGUE_REST_DAY", "Registrar este treino como descanso hoje."));

            var targetDate = session.ScheduledFor.AddDays(1);
            var targetOccupied = await db.WorkoutSessions.AnyAsync(x => x.MemberId == memberId && x.ScheduledFor == targetDate, ct);
            if (!targetOccupied)
            {
                var rescheduleAction = new CoachAction
                {
                    Id = Guid.NewGuid(), MemberId = memberId, Type = "WorkoutReschedule", Status = "Proposed", SafetyLevel = ExerciseSubstitutionTool.SafetyLevel, CreatedAt = input.CreatedAt.AddTicks(3),
                    PayloadJson = JsonSerializer.Serialize(new WorkoutReschedulePayload(session.Id, session.ScheduledFor, targetDate, "FATIGUE_RESCHEDULE_NEXT_DAY")),
                };
                db.CoachActions.Add(rescheduleAction);
                outputs.Add(FatigueProposalMessage(conversation, rescheduleAction, "Remarcar treino", "FATIGUE_RESCHEDULE_NEXT_DAY", "Remarcar este treino para o próximo dia disponível."));
            }
        }
        db.CoachMessages.Add(input);
        db.CoachMessages.AddRange(outputs);
        await db.SaveChangesAsync(ct);
    }
    private static CoachMessage FatigueProposalMessage(Conversation conversation, CoachAction action, string proposalType, string reasonCode, string content) => new()
    {
        Id = Guid.NewGuid(), Conversation = conversation, Role = "Assistant", Kind = CoachMessageKinds.ActionProposal, Content = content,
        MetadataJson = JsonSerializer.Serialize(new { reasonCode, messageType = CoachMessageKinds.ActionProposal, requiresUserInput = false, requiresConfirmation = true, actionId = action.Id, proposalType, safetyLevel = action.SafetyLevel }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        CreatedAt = action.CreatedAt,
    };
    private static CoachActionDto Action(CoachAction action) => new(action.Id, action.Type, action.Status, action.SafetyLevel, action.PayloadJson, action.CreatedAt);
    private static ExerciseSubstitutionPayload? DeserializeExerciseSubstitutionPayload(string payloadJson)
    {
        try { return JsonSerializer.Deserialize<ExerciseSubstitutionPayload>(payloadJson); }
        catch (JsonException) { return null; }
    }
    private static WorkoutReschedulePayload? DeserializeWorkoutReschedulePayload(string payloadJson)
    {
        try { return JsonSerializer.Deserialize<WorkoutReschedulePayload>(payloadJson); }
        catch (JsonException) { return null; }
    }
    private static WorkoutRestPayload? DeserializeWorkoutRestPayload(string payloadJson)
    {
        try { return JsonSerializer.Deserialize<WorkoutRestPayload>(payloadJson); }
        catch (JsonException) { return null; }
    }
    private sealed record ExerciseSubstitutionPayload(Guid SessionId, Guid WorkoutSessionExerciseId, Guid ReplacementExerciseId, string ReasonCode = ExerciseAlternativesEngine.SamePrimaryMuscleGroupReasonCode);
    private sealed record WorkoutReschedulePayload(Guid SessionId, DateOnly SourceScheduledFor, DateOnly TargetDate, string ReasonCode);
    private sealed record WorkoutRestPayload(Guid SessionId, string ReasonCode);
}
