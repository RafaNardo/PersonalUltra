using System.Security.Claims;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.StudentApi.Contracts;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;

namespace PersonalUltra.StudentApi.Endpoints;

public static class ApiEndpointExtensions
{
    public static void MapPersonalUltraApi(this WebApplication app)
    {
        app.MapGet("/health", async (PersonalUltraDbContext db, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                return ApiError(context, "SERVICE_UNAVAILABLE", "Database is unavailable.", StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Ok(new { status = "Healthy" });
        }).AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/api/v1/auth/dev-login", async (DevLoginRequest request, PersonalUltraDbContext db, DemoSessionTokenService sessions, TimeProvider clock, HttpContext context, CancellationToken cancellationToken) =>
            {
                // Empty is retained only as a compatibility path for the existing
                // installed demo; the M2-A login screen always submits an email.
                var normalizedEmail = string.IsNullOrWhiteSpace(request.Email) ? DemoIds.Email : NormalizeEmail(request.Email);
                if (normalizedEmail is null)
                    return ApiError(context, "VALIDATION_ERROR", "A valid email address is required.", StatusCodes.Status400BadRequest);

                var member = await db.Members.Include(x => x.AuthUser)
                    .SingleOrDefaultAsync(x => x.AuthUser.Email == normalizedEmail, cancellationToken);
                var isNewMember = member is null;
                if (member is null)
                {
                    var now = clock.GetUtcNow();
                    var user = new AuthUser { Id = Guid.NewGuid(), Email = normalizedEmail, CreatedAt = now };
                    member = new Member
                    {
                        Id = Guid.NewGuid(), AuthUser = user, AuthUserId = user.Id,
                        // M2-A-2 replaces this neutral placeholder with the
                        // learner's profile; do not infer a name from the email.
                        FirstName = "Aluno", LastName = "SVR", CreatedAt = now
                    };
                    db.Add(member);
                    await db.SaveChangesAsync(cancellationToken);
                }

                return Results.Ok(new DevLoginResponse(sessions.Create(member.Id), "Bearer", ToMember(member), isNewMember));
            }).AllowAnonymous();
        }

        var api = app.MapGroup("/api/v1").RequireAuthorization();

        api.MapGet("/bootstrap", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var memberId = MemberId(user);
            var member = await db.Members.Include(x => x.AuthUser).SingleAsync(x => x.Id == memberId, cancellationToken);
            var plan = await ActivePlan(db, memberId, cancellationToken);
            var nextRoute = plan is not null ? "Home" : member.OnboardingCompletedAt is null ? "Onboarding" : "PreparePlan";
            return Results.Ok(new BootstrapResponse(ToMember(member), plan is null ? null : ToPlan(plan), nextRoute));
        });

        api.MapGet("/home", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var memberId = MemberId(user);
            var plan = await ActivePlan(db, memberId, cancellationToken);
            if (plan is null) return ApiError("NO_ACTIVE_PLAN", "No active plan was found.", StatusCodes.Status404NotFound);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var session = await db.WorkoutSessions.Include(x => x.WorkoutTemplate).Include(x => x.Exercises)
                .SingleOrDefaultAsync(x => x.MemberId == memberId && x.ScheduledFor == today, cancellationToken);
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var completed = await db.WorkoutSessions.CountAsync(x => x.MemberId == memberId && x.Status == "Completed" && x.ScheduledFor >= weekStart && x.ScheduledFor < weekStart.AddDays(7), cancellationToken);
            return Results.Ok(new HomeResponse(
                $"Olá, {plan.Member.FirstName}", ToPlan(plan),
                session is null ? null : new TodayWorkoutSummaryDto(session.Id, session.WorkoutTemplate.Name, session.Status, session.Exercises.Count), completed));
        });

        api.MapGet("/training/today", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var memberId = MemberId(user);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var session = await db.WorkoutSessions.Include(x => x.WorkoutTemplate).Include(x => x.Exercises).ThenInclude(x => x.Exercise)
                .Include(x => x.Exercises).ThenInclude(x => x.SetPerformances)
                .SingleOrDefaultAsync(x => x.MemberId == memberId && x.ScheduledFor == today, cancellationToken);
            return session is null
                ? ApiError("NO_ACTIVE_PLAN", "No workout is scheduled for today.", StatusCodes.Status404NotFound)
                : Results.Ok(ToTrainingToday(session));
        });

        api.MapGet("/training/plan", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var plan = await db.Plans.AsNoTracking()
                .Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates).ThenInclude(x => x.Exercises).ThenInclude(x => x.Exercise)
                .SingleOrDefaultAsync(x => x.MemberId == MemberId(user) && x.Status == "Active", cancellationToken);
            if (plan is null) return ApiError("NO_ACTIVE_PLAN", "No active plan was found.", StatusCodes.Status404NotFound);

            return Results.Ok(new TrainingPlanResponse(plan.Name, plan.TrainingPlan.SessionsPerWeek,
                plan.TrainingPlan.WorkoutTemplates.OrderBy(template => template.Sequence)
                    .Select(template => new TrainingPlanWorkoutDto(template.Id, template.Name, template.Sequence,
                        template.Exercises.OrderBy(exercise => exercise.Sequence)
                            .Select(exercise => new TrainingPlanExerciseDto(exercise.Id, exercise.Exercise.Name, exercise.Exercise.PrimaryMuscleGroup, exercise.Sequence, exercise.PrescribedSets, exercise.MinimumRepetitions, exercise.MaximumRepetitions, exercise.RestSeconds)).ToArray()))
                    .ToArray()));
        });

        api.MapPost("/training/sessions/{id:guid}/start", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var session = await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == id && x.MemberId == MemberId(user), cancellationToken);
            if (session is null) return ApiError("VALIDATION_ERROR", "Workout session was not found.", StatusCodes.Status404NotFound);
            if (session.Status == "Completed") return ApiError("WORKOUT_ALREADY_COMPLETED", "Workout has already been completed.", StatusCodes.Status409Conflict);
            if (session.Status == "InProgress") return Results.Ok(new StartWorkoutResponse(session.Id, session.Status, session.StartedAt!.Value, true));

            session.Status = "InProgress";
            session.StartedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new StartWorkoutResponse(session.Id, session.Status, session.StartedAt.Value, false));
        });

        api.MapPost("/training/sessions/{sessionId:guid}/exercises/{sessionExerciseId:guid}/sets", async (Guid sessionId, Guid sessionExerciseId, CompleteSetRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (request.ClientOperationId == Guid.Empty || request.SetNumber < 1 || request.WeightKg < 0 || request.Repetitions < 1 || request.RepsInReserve is < 0 or > 10)
                return ApiError("VALIDATION_ERROR", "Set data is invalid.", StatusCodes.Status400BadRequest);

            var sessionExercise = await db.WorkoutSessionExercises.Include(x => x.WorkoutSession).Include(x => x.SetPerformances)
                .SingleOrDefaultAsync(x => x.Id == sessionExerciseId && x.WorkoutSessionId == sessionId && x.WorkoutSession.MemberId == MemberId(user), cancellationToken);
            if (sessionExercise is null) return ApiError("VALIDATION_ERROR", "Workout exercise was not found.", StatusCodes.Status404NotFound);

            var existing = sessionExercise.SetPerformances.SingleOrDefault(x => x.ClientOperationId == request.ClientOperationId);
            if (existing is not null) return Results.Ok(ToSet(existing, true));
            if (sessionExercise.WorkoutSession.Status != "InProgress") return ApiError("VALIDATION_ERROR", "Workout must be started before logging sets.", StatusCodes.Status409Conflict);
            if (request.SetNumber > sessionExercise.PrescribedSets || sessionExercise.SetPerformances.Any(x => x.SetNumber == request.SetNumber))
                return ApiError("VALIDATION_ERROR", "Set number is not available.", StatusCodes.Status409Conflict);

            var performance = new SetPerformance
            {
                Id = Guid.NewGuid(), ClientOperationId = request.ClientOperationId, SetNumber = request.SetNumber,
                WeightKg = request.WeightKg, Repetitions = request.Repetitions, RepsInReserve = request.RepsInReserve,
                CompletedAt = clock.GetUtcNow(), WorkoutSessionExerciseId = sessionExercise.Id
            };
            db.SetPerformances.Add(performance);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/training/sessions/{sessionId}/exercises/{sessionExerciseId}/sets/{performance.Id}", ToSet(performance, false));
        });

        api.MapPost("/training/sessions/{id:guid}/complete", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var session = await db.WorkoutSessions.Include(x => x.Exercises).ThenInclude(x => x.SetPerformances)
                .SingleOrDefaultAsync(x => x.Id == id && x.MemberId == MemberId(user), cancellationToken);
            if (session is null) return ApiError("VALIDATION_ERROR", "Workout session was not found.", StatusCodes.Status404NotFound);
            if (session.Status == "Completed") return ApiError("WORKOUT_ALREADY_COMPLETED", "Workout has already been completed.", StatusCodes.Status409Conflict);
            if (session.Status != "InProgress") return ApiError("VALIDATION_ERROR", "Workout must be started before completion.", StatusCodes.Status409Conflict);

            session.Status = "Completed";
            session.CompletedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new CompleteWorkoutResponse(session.Id, session.Status, session.CompletedAt.Value, session.Exercises.Sum(x => x.SetPerformances.Count)));
        });
    }

    private static async Task<Plan?> ActivePlan(PersonalUltraDbContext db, Guid memberId, CancellationToken cancellationToken) =>
        await db.Plans.Include(x => x.Member).Include(x => x.TrainingPlan)
            .SingleOrDefaultAsync(x => x.MemberId == memberId && x.Status == "Active", cancellationToken);

    private static Guid MemberId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static MemberDto ToMember(Member member) => new(member.Id, member.FirstName, member.LastName, member.AuthUser.Email);
    private static ActivePlanDto ToPlan(Plan plan) => new(plan.Id, plan.Name, plan.TrainingPlan.SessionsPerWeek, plan.ReviewDueAt);
    private static TrainingTodayResponse ToTrainingToday(WorkoutSession session) => new(session.Id, session.WorkoutTemplate.Name, session.Status, session.ScheduledFor, session.StartedAt,
        session.Exercises.OrderBy(x => x.Sequence).Select(x => new WorkoutExerciseDto(x.Id, x.Exercise.Name, x.Exercise.PrimaryMuscleGroup, x.Sequence, x.PrescribedSets, x.MinimumRepetitions, x.MaximumRepetitions, x.RestSeconds, x.SetPerformances.Count)).ToArray());
    private static CompleteSetResponse ToSet(SetPerformance set, bool wasAlreadyProcessed) => new(set.Id, set.ClientOperationId, set.SetNumber, set.WeightKg, set.Repetitions, set.RepsInReserve, set.CompletedAt, wasAlreadyProcessed);
    internal static IResult ApiError(string code, string message, int status) => Results.Json(new ErrorResponse(code, message, null, ActivityTraceId()), statusCode: status);
    private static IResult ApiError(HttpContext context, string code, string message, int status) => Results.Json(new ErrorResponse(code, message, null, context.TraceIdentifier), statusCode: status);
    private static string ActivityTraceId() => System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

    private static string? NormalizeEmail(string? input)
    {
        var email = input?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320) return null;
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ? email : null;
        }
        catch (FormatException) { return null; }
    }
}
