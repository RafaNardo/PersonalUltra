using Microsoft.EntityFrameworkCore;
using SvrMethod.Api.Domain;

namespace SvrMethod.Api.Infrastructure;

/// <summary>
/// Deletes the complete demonstration footprint for one member only. Catalog,
/// methodology and the original demo account are deliberately never touched.
/// </summary>
public sealed class MemberDemoResetService(SvrDbContext db)
{
    public async Task<MemberDemoResetResult> ResetAsync(Guid memberId, CancellationToken cancellationToken)
    {
        if (memberId == DemoIds.MemberId)
            return MemberDemoResetResult.BaseDemoAccount;

        var member = await db.Members.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == memberId, cancellationToken);
        if (member is null)
            return MemberDemoResetResult.MemberNotFound;

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // The relationships intentionally use member/plan predicates at every
        // level. Do not replace these with a database-wide seed reset.
        await DeleteAsync(db.CoachMessages.Where(x => x.Conversation.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.Conversations.Where(x => x.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.CoachActions.Where(x => x.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.PainReports.Where(x => x.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.DailyLogs.Where(x => x.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.WeightEntries.Where(x => x.MemberId == memberId), cancellationToken);

        await DeleteAsync(db.SetPerformances.Where(x => x.WorkoutSessionExercise.WorkoutSession.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.WorkoutSessionExercises.Where(x => x.WorkoutSession.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.WorkoutSessions.Where(x => x.MemberId == memberId), cancellationToken);

        await DeleteAsync(db.MealTemplateFoods.Where(x => x.MealTemplate.NutritionPlan.Plan.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.MealTemplates.Where(x => x.NutritionPlan.Plan.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.NutritionPlans.Where(x => x.Plan.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.WorkoutTemplateExercises.Where(x => x.WorkoutTemplate.TrainingPlan.Plan.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.WorkoutTemplates.Where(x => x.TrainingPlan.Plan.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.TrainingPlans.Where(x => x.Plan.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.Plans.Where(x => x.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.MemberProfiles.Where(x => x.MemberId == memberId), cancellationToken);
        await DeleteAsync(db.Members.Where(x => x.Id == memberId), cancellationToken);
        await DeleteAsync(db.AuthUsers.Where(x => x.Id == member.AuthUserId), cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return MemberDemoResetResult.Reset;
    }

    private async Task DeleteAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken)
        where TEntity : class
    {
        // ExecuteDelete is fast for PostgreSQL; the in-memory integration
        // provider intentionally uses the tracked equivalent because it cannot
        // translate it.
        if (db.Database.IsRelational())
        {
            await query.ExecuteDeleteAsync(cancellationToken);
            return;
        }

        db.RemoveRange(await query.ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }
}

public enum MemberDemoResetResult
{
    Reset,
    BaseDemoAccount,
    MemberNotFound,
}
