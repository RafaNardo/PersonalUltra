using Microsoft.EntityFrameworkCore;
using SvrMethod.Api.Infrastructure;

namespace SvrMethod.Api.Application.Coach;

public sealed record CoachMemberContext(string FirstName);
public sealed record CoachPlanContext(string Name, int SessionsPerWeek, DateOnly StartsOn);
public sealed record CoachTodayWorkoutContext(string Name, string Status, int ExerciseCount, int CompletedSets);
public sealed record CoachNutritionContext(int CaloriesTarget, int MealsCompleted, int MealsTotal);
public sealed record CoachProgressContext(decimal? CurrentWeightKg, int CompletedWorkouts, int ConsistencyPercent);
public sealed record CoachSafetyContext(bool HasRecentPain, string? MostRecentPainSafetyLevel);

// This is a deliberately small, projection-only read model. It may be passed to
// a responder, but it is not a domain decision and it never exposes EF entities.
public sealed record CoachContext(
    CoachMemberContext Member,
    CoachPlanContext? ActivePlan,
    CoachTodayWorkoutContext? TodayWorkout,
    CoachNutritionContext? TodayNutrition,
    CoachProgressContext Progress,
    CoachSafetyContext Safety)
{
    public CoachContext(string memberName, string? activePlanName, int completedWorkouts, bool hasRecentPain)
        : this(
            new CoachMemberContext(memberName),
            activePlanName is null ? null : new CoachPlanContext(activePlanName, 0, default),
            null,
            null,
            new CoachProgressContext(null, completedWorkouts, 0),
            new CoachSafetyContext(hasRecentPain, null))
    {
    }

    // Compatibility helpers for the deterministic responder. New consumers use
    // the structured sections above rather than reconstructing domain state.
    public string MemberName => Member.FirstName;
    public string? ActivePlanName => ActivePlan?.Name;
    public int CompletedWorkouts => Progress.CompletedWorkouts;
    public bool HasRecentPain => Safety.HasRecentPain;
}
public sealed record CoachReply(string Kind, string Content, string ReasonCode);

// Deliberately provider-agnostic: a future LLM adapter receives context and may only
// return a structured reply. Method rules and confirmed application actions stay local.
public interface ICoachResponder
{
    Task<CoachReply> ReplyAsync(string userMessage, CoachContext context, CancellationToken cancellationToken);
}

public sealed class DeterministicCoachResponder : ICoachResponder
{
    public Task<CoachReply> ReplyAsync(string userMessage, CoachContext context, CancellationToken cancellationToken)
    {
        var lower = userMessage.ToLowerInvariant();
        if (lower.Contains("dor")) return Task.FromResult(new CoachReply("Choice", "Senti sua dor. Registre região e intensidade de 0 a 10 para que possamos agir com segurança.", "PAIN_TRIAGE_REQUIRED"));
        if (lower.Contains("trocar") || lower.Contains("substit"))
        {
            if (IsFoodChangeRequest(lower))
                return Task.FromResult(new CoachReply("Text", "Para trocar um alimento, abra Nutrição, toque na refeição e escolha Trocar ao lado do item. Lá eu apresento somente alternativas equivalentes aprovadas.", "FOOD_SUBSTITUTION_IN_MEAL_SCREEN"));

            return Task.FromResult(new CoachReply("Choice", "Qual exercício você quer trocar? Abra o treino, selecione o exercício e escolha uma alternativa aprovada. A confirmação só aparece quando houver uma mudança concreta para revisar.", "EXERCISE_SELECTION_REQUIRED"));
        }
        var plan = context.ActivePlanName is null ? "seu próximo passo" : $"o plano {context.ActivePlanName}";
        return Task.FromResult(new CoachReply("Text", $"SVR Coach: vamos manter o foco em {plan}. Você já concluiu {context.CompletedWorkouts} treinos registrados.", "DETERMINISTIC_DEMO_COACH"));
    }

    private static bool IsFoodChangeRequest(string message) =>
        message.Contains("refeição") || message.Contains("refeicao") || message.Contains("almoço") || message.Contains("almoco") ||
        message.Contains("café") || message.Contains("cafe") || message.Contains("arroz") || message.Contains("alimento") || message.Contains("comida");
}

public sealed class CoachContextBuilder(SvrDbContext db, TimeProvider clock)
{
    public async Task<CoachContext> BuildAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var member = await db.Members.AsNoTracking()
            .Where(x => x.Id == memberId)
            .Select(x => new CoachMemberContext(x.FirstName))
            .SingleAsync(cancellationToken);
        var plan = await db.Plans.AsNoTracking()
            .Where(x => x.MemberId == memberId && x.Status == "Active")
            .Select(x => new { x.Id, Context = new CoachPlanContext(x.Name, x.TrainingPlan.SessionsPerWeek, x.StartsOn) })
            .SingleOrDefaultAsync(cancellationToken);
        var workout = await db.WorkoutSessions.AsNoTracking()
            .Where(x => x.MemberId == memberId && x.ScheduledFor == today)
            .Select(x => new CoachTodayWorkoutContext(
                x.WorkoutTemplate.Name,
                x.Status,
                x.Exercises.Count,
                x.Exercises.SelectMany(exercise => exercise.SetPerformances).Count()))
            .SingleOrDefaultAsync(cancellationToken);

        CoachNutritionContext? nutrition = null;
        if (plan is not null)
        {
            var nutritionPlan = await db.NutritionPlans.AsNoTracking()
                .Where(x => x.PlanId == plan.Id)
                .Select(x => new { x.CaloriesTarget, MealsTotal = x.Meals.Count })
                .SingleOrDefaultAsync(cancellationToken);
            if (nutritionPlan is not null)
            {
                var mealsCompleted = await db.DailyLogs.AsNoTracking().CountAsync(x =>
                    x.MemberId == memberId && x.Date == today && x.Completed && x.MealTemplate.NutritionPlan.PlanId == plan.Id,
                    cancellationToken);
                nutrition = new CoachNutritionContext(nutritionPlan.CaloriesTarget, mealsCompleted, nutritionPlan.MealsTotal);
            }
        }

        var completedWorkouts = await db.WorkoutSessions.AsNoTracking()
            .CountAsync(x => x.MemberId == memberId && x.Status == "Completed", cancellationToken);
        var scheduledWorkouts = await db.WorkoutSessions.AsNoTracking()
            .CountAsync(x => x.MemberId == memberId && x.ScheduledFor <= today, cancellationToken);
        var currentWeight = await db.WeightEntries.AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.RecordedAt)
            .Select(x => (decimal?)x.WeightKg)
            .FirstOrDefaultAsync(cancellationToken);
        var recentPain = await db.PainReports.AsNoTracking()
            .Where(x => x.MemberId == memberId && x.ReportedAt >= now.AddDays(-7))
            .OrderByDescending(x => x.ReportedAt)
            .Select(x => x.SafetyLevel)
            .FirstOrDefaultAsync(cancellationToken);
        var consistency = scheduledWorkouts == 0 ? 0 : (int)Math.Round(completedWorkouts * 100d / scheduledWorkouts, MidpointRounding.AwayFromZero);

        return new CoachContext(
            member,
            plan?.Context,
            workout,
            nutrition,
            new CoachProgressContext(currentWeight, completedWorkouts, consistency),
            new CoachSafetyContext(recentPain is not null, recentPain));
    }
}
