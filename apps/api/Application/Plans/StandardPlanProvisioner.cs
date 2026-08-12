using Microsoft.EntityFrameworkCore;
using SvrMethod.Api.Contracts;
using SvrMethod.Api.Domain;
using SvrMethod.Api.Infrastructure;

namespace SvrMethod.Api.Application.Plans;

/// <summary>
/// Creates the first, versioned SVR plan for a member. This is deliberately a
/// deterministic catalog copy: future generators may replace its decisions,
/// but must preserve its member ownership and idempotency guarantees.
/// </summary>
public sealed class StandardPlanProvisioner(SvrDbContext db, TimeProvider clock)
{
    private const string ActiveStatus = "Active";
    private const string MethodologyCode = "SVR";
    private const string MethodologyVersion = "0.1-standard";

    public async Task<InitialPlanResponse> GetAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var plan = await LoadActivePlanAsync(memberId, cancellationToken);
        return plan is null ? InitialPlanResponse.NotProvisioned : await ToResponseAsync(plan, false, cancellationToken);
    }

    public async Task<InitialPlanResponse> ProvisionAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var existing = await LoadActivePlanAsync(memberId, cancellationToken);
        if (existing is not null) return await ToResponseAsync(existing, true, cancellationToken);

        var methodology = await EnsureCatalogAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var startedOn = today.AddDays(-56);
        var currentWeight = await db.MemberProfiles.Where(x => x.MemberId == memberId).Select(x => x.WeightKg).SingleOrDefaultAsync(cancellationToken);
        if (currentWeight <= 0) currentWeight = 68.4m;
        var exercises = await db.Exercises.ToDictionaryAsync(x => x.Name, cancellationToken);
        var foods = await db.Foods.ToDictionaryAsync(x => x.Name, cancellationToken);

        var plan = new Plan
        {
            Id = Guid.NewGuid(), MemberId = memberId, MethodologyVersionId = methodology.Id,
            Name = "SVR Foco em Glúteos e Pernas 4x", Status = ActiveStatus,
            StartsOn = startedOn, ReviewDueAt = now.AddDays(45), CreatedAt = now
        };
        var trainingPlan = new TrainingPlan { Id = Guid.NewGuid(), Plan = plan, SessionsPerWeek = 4 };
        foreach (var (name, sequence, prescriptions) in Workouts)
        {
            var template = new WorkoutTemplate { Id = Guid.NewGuid(), Name = name, Sequence = sequence, TrainingPlan = trainingPlan };
            foreach (var prescription in prescriptions)
            {
                template.Exercises.Add(new WorkoutTemplateExercise
                {
                    Id = Guid.NewGuid(), ExerciseId = exercises[prescription.Exercise].Id, Sequence = prescription.Sequence,
                    PrescribedSets = prescription.Sets, MinimumRepetitions = prescription.MinimumRepetitions,
                    MaximumRepetitions = prescription.MaximumRepetitions, RestSeconds = prescription.RestSeconds,
                    RecommendedLoadKg = prescription.RecommendedLoadKg
                });
            }
            trainingPlan.WorkoutTemplates.Add(template);
        }

        var nutrition = new NutritionPlan
        {
            Id = Guid.NewGuid(), Plan = plan, CaloriesTarget = 2600, ProteinGramsTarget = 180,
            CarbsGramsTarget = 300, FatGramsTarget = 70
        };
        foreach (var (name, sequence, items) in Meals)
        {
            var meal = new MealTemplate { Id = Guid.NewGuid(), Name = name, Sequence = sequence, NutritionPlan = nutrition };
            foreach (var item in items)
                meal.Foods.Add(new MealTemplateFood { Id = Guid.NewGuid(), FoodId = foods[item.Food].Id, QuantityGrams = item.QuantityGrams });
            nutrition.Meals.Add(meal);
        }

        var historyIndex = 0;
        foreach (var date in HistoricalTrainingDates(startedOn, today))
        {
            var template = trainingPlan.WorkoutTemplates.Single(x => x.Sequence == WorkoutSequenceFor(date));
            var completed = historyIndex is not 8 and not 19 and not 28;
            var completedAt = now.AddDays(date.DayNumber - today.DayNumber).AddHours(7);
            var session = CreateSession(memberId, template, date, completed ? "Completed" : "Skipped", completedAt, exercises);
            if (completed) AddCompletedPerformances(session, completedAt, -7.5m + historyIndex / 4m);
            db.WorkoutSessions.Add(session);
            historyIndex++;
        }

        foreach (var date in ScheduledDates(today))
        {
            var template = trainingPlan.WorkoutTemplates.Single(x => x.Sequence == WorkoutSequenceFor(date));
            db.WorkoutSessions.Add(CreateSession(memberId, template, date, "Planned", now, exercises));
        }

        AddDemoWeightHistory(memberId, currentWeight, now);

        db.AddRange(plan, trainingPlan, nutrition);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return await ToResponseAsync(await LoadActivePlanAsync(memberId, cancellationToken) ?? plan, false, cancellationToken);
        }
        catch (DbUpdateException) when (db.Database.IsRelational())
        {
            // The partial unique index on an active plan is the concurrency gate.
            // A concurrent request that won the race has already committed a full plan.
            db.ChangeTracker.Clear();
            var concurrentPlan = await LoadActivePlanAsync(memberId, cancellationToken);
            if (concurrentPlan is not null) return await ToResponseAsync(concurrentPlan, true, cancellationToken);
            throw;
        }
    }

    private async Task<MethodologyVersion> EnsureCatalogAsync(CancellationToken cancellationToken, bool retried = false)
    {
        var methodology = await db.MethodologyVersions.SingleOrDefaultAsync(x => x.Code == MethodologyCode && x.Version == MethodologyVersion, cancellationToken);
        if (methodology is null)
        {
            methodology = new MethodologyVersion { Id = Guid.NewGuid(), Code = MethodologyCode, Version = MethodologyVersion, IsActive = true, PublishedAt = clock.GetUtcNow() };
            methodology.Rules.Add(new MethodologyRule { Id = Guid.NewGuid(), Code = "SVR-INITIAL-PLAN-001", RuleType = "InitialPlan", DefinitionJson = "{\"source\":\"svr-standard-0.1\",\"kind\":\"deterministic\"}" });
            db.MethodologyVersions.Add(methodology);
        }

        var exerciseNames = Exercises.Select(x => x.Name).ToHashSet();
        var presentExercises = await db.Exercises.Where(x => exerciseNames.Contains(x.Name)).Select(x => x.Name).ToListAsync(cancellationToken);
        foreach (var exercise in Exercises.Where(x => !presentExercises.Contains(x.Name)))
            db.Exercises.Add(new Exercise { Id = Guid.NewGuid(), Name = exercise.Name, PrimaryMuscleGroup = exercise.MuscleGroup });

        var foodNames = Foods.Select(x => x.Name).ToHashSet();
        var presentFoods = await db.Foods.Where(x => foodNames.Contains(x.Name)).Select(x => x.Name).ToListAsync(cancellationToken);
        foreach (var food in Foods.Where(x => !presentFoods.Contains(x.Name)))
            db.Foods.Add(new Food { Id = Guid.NewGuid(), Name = food.Name, Category = food.Category, CaloriesPer100g = food.Calories, ProteinPer100g = food.Protein, CarbsPer100g = food.Carbs, FatPer100g = food.Fat });

        try
        {
            if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(cancellationToken);
            return methodology;
        }
        catch (DbUpdateException) when (db.Database.IsRelational() && !retried)
        {
            // The global catalog is also protected by unique keys. A competing
            // first provision may have inserted it; reload that committed catalog.
            db.ChangeTracker.Clear();
            return await EnsureCatalogAsync(cancellationToken, true);
        }
    }

    private Task<Plan?> LoadActivePlanAsync(Guid memberId, CancellationToken cancellationToken) =>
        db.Plans.AsNoTracking().Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates)
            .Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates).ThenInclude(x => x.Exercises)
            .Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates).ThenInclude(x => x.Exercises).ThenInclude(x => x.Exercise)
            .Include(x => x.Member).Where(x => x.MemberId == memberId && x.Status == ActiveStatus).SingleOrDefaultAsync(cancellationToken);

    private async Task<InitialPlanResponse> ToResponseAsync(Plan plan, bool wasAlreadyProvisioned, CancellationToken cancellationToken)
    {
        var nutrition = await db.NutritionPlans.AsNoTracking().Include(x => x.Meals).SingleAsync(x => x.PlanId == plan.Id, cancellationToken);
        return new InitialPlanResponse(
            true, plan.Id, plan.Name, plan.TrainingPlan.SessionsPerWeek, plan.StartsOn, plan.ReviewDueAt,
            plan.TrainingPlan.WorkoutTemplates.OrderBy(x => x.Sequence).Select(x => new InitialPlanWorkoutDto(x.Id, x.Name, x.Sequence, x.Exercises.Count)).ToArray(),
            new InitialPlanNutritionDto(nutrition.CaloriesTarget, nutrition.ProteinGramsTarget, nutrition.CarbsGramsTarget, nutrition.FatGramsTarget, nutrition.Meals.OrderBy(x => x.Sequence).Select(x => x.Name).ToArray()),
            wasAlreadyProvisioned);
    }

    private static IEnumerable<DateOnly> ScheduledDates(DateOnly today)
    {
        for (var day = today; day < today.AddDays(21); day = day.AddDays(1))
            if (day.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Friday) yield return day;
    }

    private static IEnumerable<DateOnly> HistoricalTrainingDates(DateOnly startsOn, DateOnly today)
    {
        for (var day = startsOn; day < today; day = day.AddDays(1))
            if (day.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Friday) yield return day;
    }

    private static WorkoutSession CreateSession(Guid memberId, WorkoutTemplate template, DateOnly scheduledFor, string status, DateTimeOffset timestamp, IReadOnlyDictionary<string, Exercise> exercises)
    {
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(), MemberId = memberId, WorkoutTemplate = template, ScheduledFor = scheduledFor,
            Status = status, StartedAt = status == "Completed" ? timestamp.AddHours(-1) : null,
            CompletedAt = status == "Completed" ? timestamp : null, CreatedAt = timestamp
        };
        foreach (var item in template.Exercises.OrderBy(x => x.Sequence))
        {
            var exercise = exercises.Values.Single(x => x.Id == item.ExerciseId);
            session.Exercises.Add(new WorkoutSessionExercise
            {
                Id = Guid.NewGuid(), ExerciseId = item.ExerciseId, Sequence = item.Sequence,
                PrescribedSets = item.PrescribedSets, MinimumRepetitions = item.MinimumRepetitions,
                MaximumRepetitions = item.MaximumRepetitions, RestSeconds = item.RestSeconds,
                RecommendedLoadKg = item.RecommendedLoadKg,
                ExerciseSnapshotJson = $$"""{"name":"{{exercise.Name}}","source":"svr-standard-0.1"}"""
            });
        }
        return session;
    }

    private void AddDemoWeightHistory(Guid memberId, decimal currentWeight, DateTimeOffset now)
    {
        var weights = new[] { 2.8m, 2.4m, 2.1m, 1.7m, 1.4m, 1.0m, .7m, .3m, 0m };
        var daysAgo = new[] { 56, 49, 42, 35, 28, 21, 14, 7, 0 };
        for (var index = 0; index < weights.Length; index++)
            db.WeightEntries.Add(new WeightEntry { Id = Guid.NewGuid(), MemberId = memberId, WeightKg = currentWeight + weights[index], RecordedAt = now.AddDays(-daysAgo[index]) });
    }

    private static void AddCompletedPerformances(WorkoutSession session, DateTimeOffset completedAt, decimal loadAdjustment)
    {
        foreach (var sessionExercise in session.Exercises)
        {
            for (var setNumber = 1; setNumber <= sessionExercise.PrescribedSets; setNumber++)
            {
                sessionExercise.SetPerformances.Add(new SetPerformance
                {
                    Id = Guid.NewGuid(), ClientOperationId = Guid.NewGuid(), SetNumber = setNumber,
                    WeightKg = Math.Max(1, sessionExercise.RecommendedLoadKg + loadAdjustment),
                    Repetitions = sessionExercise.MinimumRepetitions + 1, RepsInReserve = 2,
                    CompletedAt = completedAt.AddMinutes(setNumber * 8)
                });
            }
        }
    }

    private static int WorkoutSequenceFor(DateOnly day) => day.DayOfWeek switch
    {
        DayOfWeek.Monday => 1, DayOfWeek.Tuesday => 2, DayOfWeek.Thursday => 3, DayOfWeek.Friday => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };

    private sealed record ExerciseSeed(string Name, string MuscleGroup);
    private sealed record FoodSeed(string Name, string Category, decimal Calories, decimal Protein, decimal Carbs, decimal Fat);
    private sealed record PrescriptionSeed(string Exercise, int Sequence, int Sets, int MinimumRepetitions, int MaximumRepetitions, int RestSeconds, decimal RecommendedLoadKg);
    private sealed record MealFoodSeed(string Food, decimal QuantityGrams);

    private static readonly ExerciseSeed[] Exercises =
    [
        new("Supino reto com barra", "Peito"), new("Remada baixa", "Costas"), new("Agachamento livre", "Quadríceps"), new("Levantamento terra romeno", "Posterior de coxa"), new("Desenvolvimento com halteres", "Ombros"), new("Elevação lateral com halteres", "Ombros"), new("Puxada dorsal na máquina", "Costas"), new("Tríceps na polia com corda", "Tríceps"), new("Rosca direta com barra", "Bíceps"), new("Elevação pélvica com barra", "Glúteos"), new("Abdução de quadril na máquina", "Glúteos"), new("Coice no cabo", "Glúteos"), new("Coice com caneleira", "Glúteos"), new("Frog pump", "Glúteos"), new("Ponte de glúteos", "Glúteos"), new("Agachamento goblet", "Quadríceps"), new("Leg press 45°", "Quadríceps"), new("Cadeira extensora", "Quadríceps"), new("Afundo com halteres", "Quadríceps"), new("Step-up com halteres", "Quadríceps"), new("Passada com halteres", "Quadríceps"), new("Stiff com barra", "Posterior de coxa"), new("Cadeira flexora", "Posterior de coxa"), new("Agachamento sumô", "Glúteos"), new("Pull through no cabo", "Glúteos"), new("Elevação pélvica unilateral com barra", "Glúteos"), new("Ponte de glúteo unilateral", "Glúteos"), new("Abdução com elástico", "Glúteos")
    ];

    private static readonly FoodSeed[] Foods =
    [
        new("Ovo cozido", "Proteína", 143, 13, 1.1m, 9.5m), new("Frango grelhado", "Proteína", 165, 31, 0, 3.6m), new("Patinho moído", "Proteína", 219, 26, 0, 12), new("Pão francês", "Carboidrato", 300, 9, 58, 3.1m), new("Aveia em flocos", "Carboidrato", 389, 17, 66, 7), new("Arroz branco cozido", "Carboidrato", 130, 2.7m, 28, .3m), new("Batata inglesa cozida", "Carboidrato", 87, 1.9m, 20, .1m), new("Tapioca", "Carboidrato", 350, 0, 86, 0), new("Feijão carioca cozido", "Carboidrato", 76, 4.8m, 14, .5m), new("Banana prata", "Fruta", 98, 1.3m, 26, .1m), new("Maçã", "Fruta", 52, .3m, 14, .2m), new("Iogurte natural", "Laticínio", 61, 3.5m, 4.7m, 3.3m), new("Queijo cottage", "Laticínio", 98, 11, 3.4m, 4.3m), new("Queijo minas", "Laticínio", 264, 17, 3.2m, 20), new("Brócolis cozido", "Vegetal", 25, 2.1m, 4.4m, .4m), new("Salada verde", "Vegetal", 20, 1.5m, 3, .2m), new("Abobrinha cozida", "Vegetal", 15, 1.1m, 3.1m, .4m), new("Azeite de oliva", "Gordura", 884, 0, 0, 100), new("Pasta de amendoim", "Gordura", 588, 25, 20, 50)
    ];

    private static readonly (string Name, int Sequence, PrescriptionSeed[] Prescriptions)[] Workouts =
    [
        ("Superior — costas e braços", 1, [new("Puxada dorsal na máquina", 1, 3, 8, 10, 90, 42), new("Remada baixa", 2, 3, 8, 10, 90, 45), new("Desenvolvimento com halteres", 3, 3, 8, 10, 90, 16), new("Elevação lateral com halteres", 4, 3, 12, 15, 60, 6), new("Tríceps na polia com corda", 5, 3, 10, 12, 60, 18), new("Rosca direta com barra", 6, 3, 10, 12, 60, 14), new("Supino reto com barra", 7, 3, 8, 10, 90, 32)]),
        ("Glúteos 1", 2, [new("Elevação pélvica com barra", 1, 4, 8, 10, 120, 70), new("Abdução de quadril na máquina", 2, 3, 12, 15, 60, 45), new("Coice no cabo", 3, 3, 12, 15, 60, 15), new("Coice com caneleira", 4, 3, 15, 20, 45, 8), new("Frog pump", 5, 3, 15, 20, 45, 20), new("Ponte de glúteos", 6, 3, 12, 15, 60, 30), new("Abdução com elástico", 7, 3, 15, 20, 45, 8)]),
        ("Pernas — quadríceps e glúteos", 3, [new("Agachamento livre", 1, 4, 8, 10, 120, 45), new("Leg press 45°", 2, 4, 10, 12, 120, 100), new("Cadeira extensora", 3, 3, 12, 15, 60, 35), new("Afundo com halteres", 4, 3, 10, 12, 90, 12), new("Step-up com halteres", 5, 3, 10, 12, 90, 10), new("Passada com halteres", 6, 3, 12, 14, 90, 10), new("Agachamento goblet", 7, 3, 12, 15, 90, 18)]),
        ("Posteriores e glúteos", 4, [new("Stiff com barra", 1, 4, 8, 10, 120, 45), new("Levantamento terra romeno", 2, 3, 8, 10, 120, 42), new("Cadeira flexora", 3, 4, 10, 12, 75, 35), new("Agachamento sumô", 4, 3, 10, 12, 90, 28), new("Pull through no cabo", 5, 3, 12, 15, 75, 30), new("Elevação pélvica unilateral com barra", 6, 3, 10, 12, 90, 28), new("Ponte de glúteo unilateral", 7, 3, 12, 15, 60, 16)])
    ];

    private static readonly (string Name, int Sequence, MealFoodSeed[] Foods)[] Meals =
    [
        ("Café da manhã", 1, [new("Ovo cozido", 100), new("Pão francês", 75), new("Banana prata", 100)]),
        ("Lanche da manhã", 2, [new("Iogurte natural", 170), new("Aveia em flocos", 40), new("Maçã", 130)]),
        ("Almoço", 3, [new("Frango grelhado", 180), new("Arroz branco cozido", 180), new("Feijão carioca cozido", 120), new("Salada verde", 80), new("Azeite de oliva", 8)]),
        ("Pré-treino", 4, [new("Tapioca", 70), new("Queijo cottage", 80), new("Banana prata", 90)]),
        ("Jantar", 5, [new("Patinho moído", 140), new("Batata inglesa cozida", 220), new("Brócolis cozido", 100)]),
        ("Ceia", 6, [new("Iogurte natural", 170), new("Pasta de amendoim", 15)])
    ];
}
