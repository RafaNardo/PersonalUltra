using Microsoft.EntityFrameworkCore;
using SvrMethod.Api.Domain;

namespace SvrMethod.Api.Infrastructure;

public sealed class DemoDataSeeder(SvrDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<bool> IsSafeToResetAsync(CancellationToken cancellationToken)
    {
        var hasDemoUser = await dbContext.AuthUsers.AnyAsync(user => user.Id == DemoIds.UserId, cancellationToken);
        if (!hasDemoUser) return false;

        var hasNonDemoUsers = await dbContext.AuthUsers.AnyAsync(user => user.Id != DemoIds.UserId, cancellationToken);
        var hasNonDemoMembers = await dbContext.Members.AnyAsync(member => member.Id != DemoIds.MemberId, cancellationToken);
        return !hasNonDemoUsers && !hasNonDemoMembers;
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        if (!await IsSafeToResetAsync(cancellationToken))
            throw new InvalidOperationException("Demo reset requires an isolated demo database.");
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.AuthUsers.AnyAsync(user => user.Id == DemoIds.UserId, cancellationToken))
        {
            var existingPlan = await dbContext.Plans.Include(x => x.TrainingPlan).ThenInclude(x => x.WorkoutTemplates).ThenInclude(x => x.Exercises).ThenInclude(x => x.Exercise)
                .SingleOrDefaultAsync(x => x.MemberId == DemoIds.MemberId && x.Status == "Active", cancellationToken);
            if (existingPlan is not null)
            {
                existingPlan.Name = "SVR Foco em Glúteos e Pernas 4x";
                foreach (var template in existingPlan.TrainingPlan.WorkoutTemplates)
                {
                    template.Name = template.Sequence switch { 1 => "Superior — costas e braços", 2 => "Glúteos 1", 3 => "Pernas — quadríceps e glúteos", 4 => "Posteriores e glúteos", _ => template.Name };
                }
                var existingMember = await dbContext.Members.SingleAsync(x => x.Id == DemoIds.MemberId, cancellationToken);
                await EnsureRealisticNutritionAsync(existingPlan, existingMember, timeProvider.GetUtcNow(), cancellationToken);
                await EnsureDemoProgressHistoryAsync(existingPlan, existingMember, timeProvider.GetUtcNow(), cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return;
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var methodology = new MethodologyVersion
        {
            Id = Guid.NewGuid(), Code = "SVR", Version = "0.1-demo", IsActive = true, PublishedAt = now
        };
        methodology.Rules.Add(new MethodologyRule
        {
            Id = Guid.NewGuid(), Code = "SVR-PROGRESSION-001", RuleType = "Progression",
            DefinitionJson = "{\"strategy\":\"double-progression\",\"reasonCode\":\"DEMO_APPROVED_RULE\"}"
        });

        var member = new Member
        {
            Id = DemoIds.MemberId, AuthUserId = DemoIds.UserId, FirstName = "Rafa", LastName = "Silva", CreatedAt = now,
            AuthUser = new AuthUser { Id = DemoIds.UserId, Email = DemoIds.Email, CreatedAt = now }
        };
        var plan = new Plan
        {
            Id = Guid.NewGuid(), Member = member, MethodologyVersion = methodology, Name = "SVR Foco em Glúteos e Pernas 4x",
            Status = "Active", StartsOn = today.AddDays(-28), ReviewDueAt = now.AddDays(14), CreatedAt = now
        };
        var trainingPlan = new TrainingPlan { Id = Guid.NewGuid(), Plan = plan, SessionsPerWeek = 4 };
        var benchPress = Exercise("Supino reto com barra", "Peito");
        var row = Exercise("Remada baixa", "Costas");
        var squat = Exercise("Agachamento livre", "Quadríceps");
        var deadlift = Exercise("Levantamento terra romeno", "Posterior de coxa");
        var shoulderPress = Exercise("Desenvolvimento com halteres", "Ombros");
        var lateralRaise = Exercise("Elevação lateral com halteres", "Ombros");
        var latPulldown = Exercise("Puxada dorsal na máquina", "Costas");
        var tricepsRope = Exercise("Tríceps na polia com corda", "Tríceps");
        var bicepsCurl = Exercise("Rosca direta com barra", "Bíceps");
        var hipThrust = Exercise("Elevação pélvica com barra", "Glúteos");
        var hipAbductionMachine = Exercise("Abdução de quadril na máquina", "Glúteos");
        var cableKickback = Exercise("Coice no cabo", "Glúteos");
        var ankleKickback = Exercise("Coice com caneleira", "Glúteos");
        var frogPump = Exercise("Frog pump", "Glúteos");
        var gluteBridge = Exercise("Ponte de glúteos", "Glúteos");
        var gobletSquat = Exercise("Agachamento goblet", "Quadríceps");
        var legPress = Exercise("Leg press 45°", "Quadríceps");
        var legExtension = Exercise("Cadeira extensora", "Quadríceps");
        var dumbbellLunge = Exercise("Afundo com halteres", "Quadríceps");
        var stepUp = Exercise("Step-up com halteres", "Quadríceps");
        var dumbbellWalk = Exercise("Passada com halteres", "Quadríceps");
        var stiff = Exercise("Stiff com barra", "Posterior de coxa");
        var legCurl = Exercise("Cadeira flexora", "Posterior de coxa");
        var sumoSquat = Exercise("Agachamento sumô", "Glúteos");
        var pullThrough = Exercise("Pull through no cabo", "Glúteos");
        var unilateralHipThrust = Exercise("Elevação pélvica unilateral com barra", "Glúteos");
        var unilateralBridge = Exercise("Ponte de glúteo unilateral", "Glúteos");
        var bandAbduction = Exercise("Abdução com elástico", "Glúteos");

        var upper = Template("Superior — costas e braços", 1, [Prescription(latPulldown, 1, 3, 8, 10, 90, 42), Prescription(row, 2, 3, 8, 10, 90, 45), Prescription(shoulderPress, 3, 3, 8, 10, 90, 16), Prescription(lateralRaise, 4, 3, 12, 15, 60, 6), Prescription(tricepsRope, 5, 3, 10, 12, 60, 18), Prescription(bicepsCurl, 6, 3, 10, 12, 60, 14), Prescription(benchPress, 7, 3, 8, 10, 90, 32)]);
        var glutes = Template("Glúteos 1", 2, [Prescription(hipThrust, 1, 4, 8, 10, 120, 70), Prescription(hipAbductionMachine, 2, 3, 12, 15, 60, 45), Prescription(cableKickback, 3, 3, 12, 15, 60, 15), Prescription(ankleKickback, 4, 3, 15, 20, 45, 8), Prescription(frogPump, 5, 3, 15, 20, 45, 20), Prescription(gluteBridge, 6, 3, 12, 15, 60, 30), Prescription(bandAbduction, 7, 3, 15, 20, 45, 8)]);
        var legs = Template("Pernas — quadríceps e glúteos", 3, [Prescription(squat, 1, 4, 8, 10, 120, 45), Prescription(legPress, 2, 4, 10, 12, 120, 100), Prescription(legExtension, 3, 3, 12, 15, 60, 35), Prescription(dumbbellLunge, 4, 3, 10, 12, 90, 12), Prescription(stepUp, 5, 3, 10, 12, 90, 10), Prescription(dumbbellWalk, 6, 3, 12, 14, 90, 10), Prescription(gobletSquat, 7, 3, 12, 15, 90, 18)]);
        var posteriorGlutes = Template("Posteriores e glúteos", 4, [Prescription(stiff, 1, 4, 8, 10, 120, 45), Prescription(deadlift, 2, 3, 8, 10, 120, 42), Prescription(legCurl, 3, 4, 10, 12, 75, 35), Prescription(sumoSquat, 4, 3, 10, 12, 90, 28), Prescription(pullThrough, 5, 3, 12, 15, 75, 30), Prescription(unilateralHipThrust, 6, 3, 10, 12, 90, 28), Prescription(unilateralBridge, 7, 3, 12, 15, 60, 16)]);

        trainingPlan.WorkoutTemplates.Add(upper);
        trainingPlan.WorkoutTemplates.Add(glutes);
        trainingPlan.WorkoutTemplates.Add(legs);
        trainingPlan.WorkoutTemplates.Add(posteriorGlutes);
        dbContext.AddRange(methodology, member, plan, trainingPlan, benchPress, row, squat, deadlift, shoulderPress, lateralRaise, latPulldown, tricepsRope, bicepsCurl, hipThrust, hipAbductionMachine, cableKickback, ankleKickback, frogPump, gluteBridge, gobletSquat, legPress, legExtension, dumbbellLunge, stepUp, dumbbellWalk, stiff, legCurl, sumoSquat, pullThrough, unilateralHipThrust, unilateralBridge, bandAbduction);

        var previousSession = CreateSession(member, glutes, today.AddDays(-2), "Completed", now.AddDays(-2), now.AddDays(-2).AddHours(1));
        AddCompletedPerformances(previousSession, now.AddDays(-2));

        dbContext.Add(previousSession);
        dbContext.Add(CreateSession(member, upper, today, "Planned", null, null));
        AddRealisticNutrition(plan, member, now);
        AddDemoWeightEntries(member, now, new HashSet<DateOnly>());
        AddDemoCompletedSessions(member, [upper, glutes, legs, posteriorGlutes], now, new HashSet<DateOnly> { today.AddDays(-2) });
        var conversation = new Conversation { Id = Guid.NewGuid(), Member = member, CreatedAt = now.AddDays(-1) };
        conversation.Messages.Add(new CoachMessage { Id = Guid.NewGuid(), Role = "Assistant", Kind = "ProgressInsight", Content = "Sua consistência está boa. Priorize a execução do treino de hoje.", MetadataJson = "{\"reasonCode\":\"DEMO_CONSISTENCY\"}", CreatedAt = now.AddDays(-1) });
        dbContext.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        Exercise Exercise(string name, string muscleGroup) => new() { Id = Guid.NewGuid(), Name = name, PrimaryMuscleGroup = muscleGroup };

        WorkoutTemplate Template(string name, int sequence, WorkoutTemplateExercise[] exercises)
        {
            var template = new WorkoutTemplate { Id = Guid.NewGuid(), Name = name, Sequence = sequence };
            template.Exercises.AddRange(exercises);
            return template;
        }

        WorkoutTemplateExercise Prescription(Exercise exercise, int sequence, int sets, int minimumRepetitions, int maximumRepetitions, int restSeconds, decimal load) => new()
        {
            Id = Guid.NewGuid(), Exercise = exercise, Sequence = sequence, PrescribedSets = sets,
            MinimumRepetitions = minimumRepetitions, MaximumRepetitions = maximumRepetitions,
            RestSeconds = restSeconds, RecommendedLoadKg = load
        };
    }

    private static WorkoutSession CreateSession(Member member, WorkoutTemplate template, DateOnly scheduledFor, string status, DateTimeOffset? startedAt, DateTimeOffset? completedAt)
    {
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(), Member = member, WorkoutTemplate = template, ScheduledFor = scheduledFor,
            Status = status, StartedAt = startedAt, CompletedAt = completedAt, CreatedAt = startedAt ?? DateTimeOffset.UtcNow
        };
        foreach (var templateExercise in template.Exercises)
        {
            session.Exercises.Add(new WorkoutSessionExercise
            {
                Id = Guid.NewGuid(), Exercise = templateExercise.Exercise, Sequence = templateExercise.Sequence,
                PrescribedSets = templateExercise.PrescribedSets, MinimumRepetitions = templateExercise.MinimumRepetitions,
                MaximumRepetitions = templateExercise.MaximumRepetitions, RestSeconds = templateExercise.RestSeconds,
                RecommendedLoadKg = templateExercise.RecommendedLoadKg,
                ExerciseSnapshotJson = $"{{\"name\":\"{templateExercise.Exercise.Name}\",\"source\":\"demo-seed\"}}"
            });
        }

        return session;
    }

    private async Task EnsureDemoProgressHistoryAsync(Plan plan, Member member, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existingWeightDays = (await dbContext.WeightEntries.Where(x => x.MemberId == member.Id).Select(x => x.RecordedAt).ToListAsync(cancellationToken))
            .Select(x => DateOnly.FromDateTime(x.UtcDateTime)).ToHashSet();
        AddDemoWeightEntries(member, now, existingWeightDays);

        var existingSessionDays = (await dbContext.WorkoutSessions.Where(x => x.MemberId == member.Id).Select(x => x.ScheduledFor).ToListAsync(cancellationToken)).ToHashSet();
        AddDemoCompletedSessions(member, plan.TrainingPlan.WorkoutTemplates.OrderBy(x => x.Sequence).ToArray(), now, existingSessionDays);
    }

    private void AddDemoWeightEntries(Member member, DateTimeOffset now, ISet<DateOnly> existingDays)
    {
        var entries = new (int DaysAgo, decimal WeightKg)[]
        {
            (28, 82.8m), (24, 82.5m), (21, 82.4m), (18, 82.1m), (14, 82.0m),
            (10, 81.8m), (7, 81.8m), (3, 81.6m), (0, 81.5m)
        };
        foreach (var entry in entries)
        {
            var recordedAt = now.AddDays(-entry.DaysAgo);
            if (existingDays.Contains(DateOnly.FromDateTime(recordedAt.UtcDateTime))) continue;
            dbContext.WeightEntries.Add(new WeightEntry { Id = Guid.NewGuid(), Member = member, WeightKg = entry.WeightKg, RecordedAt = recordedAt });
        }
    }

    private void AddDemoCompletedSessions(Member member, IReadOnlyList<WorkoutTemplate> templates, DateTimeOffset now, ISet<DateOnly> existingDays)
    {
        var dates = new[] { -26, -24, -21, -19, -17, -14, -12, -10, -7, -2 };
        foreach (var (daysAgo, index) in dates.Select((daysAgo, index) => (daysAgo, index)))
        {
            var scheduledFor = DateOnly.FromDateTime(now.AddDays(daysAgo).UtcDateTime);
            if (existingDays.Contains(scheduledFor)) continue;
            var completedAt = now.AddDays(daysAgo).AddHours(1);
            var session = CreateSession(member, templates[index % templates.Count], scheduledFor, "Completed", completedAt.AddHours(-1), completedAt);
            AddCompletedPerformances(session, completedAt, index / 4 * 2.5m - 7.5m);
            dbContext.WorkoutSessions.Add(session);
        }
    }

    private static void AddCompletedPerformances(WorkoutSession session, DateTimeOffset completedAt, decimal loadAdjustment = 0)
    {
        foreach (var sessionExercise in session.Exercises)
        {
            for (var number = 1; number <= sessionExercise.PrescribedSets; number++)
            {
                sessionExercise.SetPerformances.Add(new SetPerformance
                {
                    Id = Guid.NewGuid(), ClientOperationId = Guid.NewGuid(), SetNumber = number,
                    WeightKg = sessionExercise.RecommendedLoadKg + loadAdjustment, Repetitions = sessionExercise.MinimumRepetitions + 1,
                    RepsInReserve = 2, CompletedAt = completedAt.AddMinutes(number * 10)
                });
            }
        }
    }

    private async Task EnsureRealisticNutritionAsync(Plan plan, Member member, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var nutrition = await dbContext.NutritionPlans.SingleOrDefaultAsync(x => x.PlanId == plan.Id, cancellationToken);
        var expectedNames = new[] { "Café da manhã", "Lanche da manhã", "Almoço", "Pré-treino", "Jantar", "Ceia" };
        var existingMealNames = nutrition is null
            ? []
            : await dbContext.MealTemplates.Where(x => x.NutritionPlanId == nutrition.Id).Select(x => x.Name).ToListAsync(cancellationToken);
        if (existingMealNames.Count == expectedNames.Length && expectedNames.All(existingMealNames.Contains)) return;

        if (nutrition is not null)
        {
            var mealIds = dbContext.MealTemplates.Where(x => x.NutritionPlanId == nutrition.Id).Select(x => x.Id);
            await dbContext.DailyLogs.Where(x => x.MemberId == member.Id && mealIds.Contains(x.MealTemplateId)).ExecuteDeleteAsync(cancellationToken);
            await dbContext.MealTemplateFoods.Where(x => mealIds.Contains(x.MealTemplateId)).ExecuteDeleteAsync(cancellationToken);
            await dbContext.MealTemplates.Where(x => x.NutritionPlanId == nutrition.Id).ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            nutrition = new NutritionPlan { Id = Guid.NewGuid(), Plan = plan, CaloriesTarget = 2600, ProteinGramsTarget = 180, CarbsGramsTarget = 300, FatGramsTarget = 70 };
            dbContext.NutritionPlans.Add(nutrition);
        }

        var existingFoods = await dbContext.Foods.ToDictionaryAsync(x => x.Name, cancellationToken);
        var foods = CreateFoods(existingFoods);
        AddMeals(nutrition, foods);
        dbContext.Add(new DailyLog { Id = Guid.NewGuid(), Member = member, Date = DateOnly.FromDateTime(now.UtcDateTime), MealTemplate = nutrition.Meals.Single(x => x.Sequence == 1), Completed = true });
        if (!await dbContext.WeightEntries.AnyAsync(x => x.MemberId == member.Id, cancellationToken))
        {
            dbContext.AddRange(new WeightEntry { Id = Guid.NewGuid(), Member = member, WeightKg = 82.4m, RecordedAt = now.AddDays(-21) }, new WeightEntry { Id = Guid.NewGuid(), Member = member, WeightKg = 81.8m, RecordedAt = now.AddDays(-7) }, new WeightEntry { Id = Guid.NewGuid(), Member = member, WeightKg = 81.5m, RecordedAt = now });
        }
    }

    private void AddRealisticNutrition(Plan plan, Member member, DateTimeOffset now)
    {
        var foods = CreateFoods(new Dictionary<string, Food>());
        var nutrition = new NutritionPlan { Id = Guid.NewGuid(), Plan = plan, CaloriesTarget = 2600, ProteinGramsTarget = 180, CarbsGramsTarget = 300, FatGramsTarget = 70 };
        AddMeals(nutrition, foods);
        dbContext.Add(nutrition);
        dbContext.Add(new DailyLog { Id = Guid.NewGuid(), Member = member, Date = DateOnly.FromDateTime(now.UtcDateTime), MealTemplate = nutrition.Meals.Single(x => x.Sequence == 1), Completed = true });
    }

    private Dictionary<string, Food> CreateFoods(Dictionary<string, Food> existing)
    {
        var seeds = new[]
        {
            new FoodSeed("Ovo cozido", "Proteína", 143, 13, 1.1m, 9.5m), new FoodSeed("Frango grelhado", "Proteína", 165, 31, 0, 3.6m), new FoodSeed("Patinho moído", "Proteína", 219, 26, 0, 12),
            new FoodSeed("Pão francês", "Carboidrato", 300, 9, 58, 3.1m), new FoodSeed("Aveia em flocos", "Carboidrato", 389, 17, 66, 7), new FoodSeed("Arroz branco cozido", "Carboidrato", 130, 2.7m, 28, .3m), new FoodSeed("Batata inglesa cozida", "Carboidrato", 87, 1.9m, 20, .1m), new FoodSeed("Tapioca", "Carboidrato", 350, 0, 86, 0), new FoodSeed("Feijão carioca cozido", "Carboidrato", 76, 4.8m, 14, .5m),
            new FoodSeed("Banana prata", "Fruta", 98, 1.3m, 26, .1m), new FoodSeed("Maçã", "Fruta", 52, .3m, 14, .2m),
            new FoodSeed("Iogurte natural", "Laticínio", 61, 3.5m, 4.7m, 3.3m), new FoodSeed("Queijo cottage", "Laticínio", 98, 11, 3.4m, 4.3m), new FoodSeed("Queijo minas", "Laticínio", 264, 17, 3.2m, 20),
            new FoodSeed("Brócolis cozido", "Vegetal", 25, 2.1m, 4.4m, .4m), new FoodSeed("Salada verde", "Vegetal", 20, 1.5m, 3, .2m), new FoodSeed("Abobrinha cozida", "Vegetal", 15, 1.1m, 3.1m, .4m),
            new FoodSeed("Azeite de oliva", "Gordura", 884, 0, 0, 100), new FoodSeed("Pasta de amendoim", "Gordura", 588, 25, 20, 50)
        };
        var foods = new Dictionary<string, Food>(existing);
        foreach (var seed in seeds)
        {
            if (foods.ContainsKey(seed.Name)) continue;
            var food = new Food { Id = Guid.NewGuid(), Name = seed.Name, Category = seed.Category, CaloriesPer100g = seed.Calories, ProteinPer100g = seed.Protein, CarbsPer100g = seed.Carbs, FatPer100g = seed.Fat };
            foods.Add(food.Name, food); dbContext.Foods.Add(food);
        }
        return foods;
    }

    private static void AddMeals(NutritionPlan nutrition, IReadOnlyDictionary<string, Food> foods)
    {
        Meal("Café da manhã", 1, ("Ovo cozido", 100), ("Pão francês", 75), ("Banana prata", 100));
        Meal("Lanche da manhã", 2, ("Iogurte natural", 170), ("Aveia em flocos", 40), ("Maçã", 130));
        Meal("Almoço", 3, ("Frango grelhado", 180), ("Arroz branco cozido", 180), ("Feijão carioca cozido", 120), ("Salada verde", 80), ("Azeite de oliva", 8));
        Meal("Pré-treino", 4, ("Tapioca", 70), ("Queijo cottage", 80), ("Banana prata", 90));
        Meal("Jantar", 5, ("Patinho moído", 140), ("Batata inglesa cozida", 220), ("Brócolis cozido", 100));
        Meal("Ceia", 6, ("Iogurte natural", 170), ("Pasta de amendoim", 15));

        void Meal(string name, int sequence, params (string Food, decimal Quantity)[] items)
        {
            var meal = new MealTemplate { Id = Guid.NewGuid(), Name = name, Sequence = sequence };
            foreach (var item in items) meal.Foods.Add(new MealTemplateFood { Id = Guid.NewGuid(), Food = foods[item.Food], QuantityGrams = item.Quantity });
            nutrition.Meals.Add(meal);
        }
    }

    private sealed record FoodSeed(string Name, string Category, decimal Calories, decimal Protein, decimal Carbs, decimal Fat);
}
