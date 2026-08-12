using Microsoft.EntityFrameworkCore;
using SvrMethod.Api.Domain;

namespace SvrMethod.Api.Infrastructure;

public sealed class SvrDbContext(DbContextOptions<SvrDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
    public DbSet<MethodologyVersion> MethodologyVersions => Set<MethodologyVersion>();
    public DbSet<MethodologyRule> MethodologyRules => Set<MethodologyRule>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<WorkoutTemplateExercise> WorkoutTemplateExercises => Set<WorkoutTemplateExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<SetPerformance> SetPerformances => Set<SetPerformance>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<MealTemplate> MealTemplates => Set<MealTemplate>();
    public DbSet<MealTemplateFood> MealTemplateFoods => Set<MealTemplateFood>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<CoachMessage> CoachMessages => Set<CoachMessage>();
    public DbSet<CoachAction> CoachActions => Set<CoachAction>();
    public DbSet<PainReport> PainReports => Set<PainReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.ToTable("users", "auth");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.HasIndex(x => x.Email).IsUnique();
        });
        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("members", "members");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.HasIndex(x => x.AuthUserId).IsUnique();
            entity.HasOne(x => x.AuthUser).WithOne(x => x.Member).HasForeignKey<Member>(x => x.AuthUserId);
        });
        modelBuilder.Entity<MemberProfile>(entity =>
        {
            entity.ToTable("profiles", "members");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Goal).HasMaxLength(100);
            entity.Property(x => x.ExperienceLevel).HasMaxLength(50);
            entity.Property(x => x.TrainingLocation).HasMaxLength(100);
            entity.Property(x => x.EquipmentNotes).HasMaxLength(1200);
            entity.Property(x => x.HealthConditions).HasMaxLength(1200);
            entity.Property(x => x.MovementRestrictions).HasMaxLength(1200);
            entity.Property(x => x.CurrentPainDescription).HasMaxLength(1200);
            entity.Property(x => x.NutritionPreferences).HasMaxLength(1200);
            entity.Property(x => x.NutritionRestrictions).HasMaxLength(1200);
            entity.Property(x => x.HeightCm).HasPrecision(5, 2);
            entity.Property(x => x.WeightKg).HasPrecision(5, 2);
            entity.HasIndex(x => x.MemberId).IsUnique();
            entity.HasOne(x => x.Member).WithOne(x => x.Profile).HasForeignKey<MemberProfile>(x => x.MemberId);
        });
        modelBuilder.Entity<MethodologyVersion>(entity =>
        {
            entity.ToTable("versions", "methodology");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Version).HasMaxLength(50);
            entity.HasIndex(x => new { x.Code, x.Version }).IsUnique();
        });
        modelBuilder.Entity<MethodologyRule>(entity =>
        {
            entity.ToTable("rules", "methodology");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.RuleType).HasMaxLength(100);
            entity.Property(x => x.DefinitionJson).HasColumnType("jsonb");
            entity.HasOne(x => x.MethodologyVersion).WithMany(x => x.Rules).HasForeignKey(x => x.MethodologyVersionId);
        });
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans", "plans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.Member).WithMany(x => x.Plans).HasForeignKey(x => x.MemberId);
            entity.HasOne(x => x.MethodologyVersion).WithMany().HasForeignKey(x => x.MethodologyVersionId);
            entity.HasIndex(x => new { x.MemberId, x.Status });
            entity.HasIndex(x => x.MemberId).HasFilter("\"Status\" = 'Active'").IsUnique();
        });
        modelBuilder.Entity<TrainingPlan>(entity =>
        {
            entity.ToTable("training_plans", "training");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Plan).WithOne(x => x.TrainingPlan).HasForeignKey<TrainingPlan>(x => x.PlanId);
        });
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("exercises", "training");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.PrimaryMuscleGroup).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<WorkoutTemplate>(entity =>
        {
            entity.ToTable("workout_templates", "training");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.HasOne(x => x.TrainingPlan).WithMany(x => x.WorkoutTemplates).HasForeignKey(x => x.TrainingPlanId);
        });
        modelBuilder.Entity<WorkoutTemplateExercise>(entity =>
        {
            entity.ToTable("workout_template_exercises", "training");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecommendedLoadKg).HasPrecision(8, 2);
            entity.HasOne(x => x.WorkoutTemplate).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutTemplateId);
            entity.HasOne(x => x.Exercise).WithMany(x => x.WorkoutTemplateExercises).HasForeignKey(x => x.ExerciseId);
            entity.HasIndex(x => new { x.WorkoutTemplateId, x.Sequence }).IsUnique();
        });
        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("workout_sessions", "training");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.Member).WithMany(x => x.WorkoutSessions).HasForeignKey(x => x.MemberId);
            entity.HasOne(x => x.WorkoutTemplate).WithMany(x => x.WorkoutSessions).HasForeignKey(x => x.WorkoutTemplateId);
            entity.HasIndex(x => new { x.MemberId, x.ScheduledFor }).IsUnique();
        });
        modelBuilder.Entity<WorkoutSessionExercise>(entity =>
        {
            entity.ToTable("workout_session_exercises", "training");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecommendedLoadKg).HasPrecision(8, 2);
            entity.Property(x => x.ExerciseSnapshotJson).HasColumnType("jsonb");
            entity.HasOne(x => x.WorkoutSession).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutSessionId);
            entity.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId);
            entity.HasIndex(x => new { x.WorkoutSessionId, x.Sequence }).IsUnique();
        });
        modelBuilder.Entity<SetPerformance>(entity =>
        {
            entity.ToTable("set_performances", "training");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WeightKg).HasPrecision(8, 2);
            entity.HasOne(x => x.WorkoutSessionExercise).WithMany(x => x.SetPerformances).HasForeignKey(x => x.WorkoutSessionExerciseId);
            entity.HasIndex(x => new { x.WorkoutSessionExerciseId, x.ClientOperationId }).IsUnique();
            entity.HasIndex(x => new { x.WorkoutSessionExerciseId, x.SetNumber }).IsUnique();
        });
        modelBuilder.Entity<WeightEntry>(entity => { entity.ToTable("weight_entries", "progress"); entity.HasKey(x => x.Id); entity.Property(x => x.WeightKg).HasPrecision(6, 2); entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId); entity.HasIndex(x => new { x.MemberId, x.RecordedAt }); });
        modelBuilder.Entity<NutritionPlan>(entity =>
        {
            entity.ToTable("nutrition_plans", "nutrition", table => table.HasCheckConstraint("CK_nutrition_plans_targets_positive", "\"CaloriesTarget\" > 0 AND \"ProteinGramsTarget\" > 0 AND \"CarbsGramsTarget\" >= 0 AND \"FatGramsTarget\" >= 0"));
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId);
            entity.HasIndex(x => x.PlanId).IsUnique();
        });
        modelBuilder.Entity<Food>(entity =>
        {
            entity.ToTable("foods", "nutrition", table => table.HasCheckConstraint("CK_foods_nutrition_nonnegative", "\"CaloriesPer100g\" > 0 AND \"ProteinPer100g\" >= 0 AND \"CarbsPer100g\" >= 0 AND \"FatPer100g\" >= 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.CaloriesPer100g).HasPrecision(7, 2);
            entity.Property(x => x.ProteinPer100g).HasPrecision(6, 2);
            entity.Property(x => x.CarbsPer100g).HasPrecision(6, 2);
            entity.Property(x => x.FatPer100g).HasPrecision(6, 2);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<MealTemplate>(entity =>
        {
            entity.ToTable("meal_templates", "nutrition", table => table.HasCheckConstraint("CK_meal_templates_sequence_positive", "\"Sequence\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasOne(x => x.NutritionPlan).WithMany(x => x.Meals).HasForeignKey(x => x.NutritionPlanId);
            entity.HasIndex(x => new { x.NutritionPlanId, x.Sequence }).IsUnique();
        });
        modelBuilder.Entity<MealTemplateFood>(entity =>
        {
            entity.ToTable("meal_template_foods", "nutrition", table => table.HasCheckConstraint("CK_meal_template_foods_quantity_positive", "\"QuantityGrams\" > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuantityGrams).HasPrecision(7, 2);
            entity.HasOne(x => x.MealTemplate).WithMany(x => x.Foods).HasForeignKey(x => x.MealTemplateId);
            entity.HasOne(x => x.Food).WithMany().HasForeignKey(x => x.FoodId);
        });
        modelBuilder.Entity<DailyLog>(entity => { entity.ToTable("daily_logs", "nutrition"); entity.HasKey(x => x.Id); entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId); entity.HasOne(x => x.MealTemplate).WithMany().HasForeignKey(x => x.MealTemplateId); entity.HasIndex(x => new { x.MemberId, x.Date, x.MealTemplateId }).IsUnique(); });
        modelBuilder.Entity<Conversation>(entity => { entity.ToTable("conversations", "coaching"); entity.HasKey(x => x.Id); entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId); entity.HasIndex(x => x.MemberId).IsUnique(); });
        modelBuilder.Entity<CoachMessage>(entity => { entity.ToTable("messages", "coaching"); entity.HasKey(x => x.Id); entity.Property(x => x.Role).HasMaxLength(20); entity.Property(x => x.Kind).HasMaxLength(50); entity.Property(x => x.MetadataJson).HasColumnType("jsonb"); entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId); entity.HasIndex(x => new { x.ConversationId, x.CreatedAt, x.Id }); });
        modelBuilder.Entity<CoachAction>(entity => { entity.ToTable("actions", "coaching"); entity.HasKey(x => x.Id); entity.Property(x => x.Type).HasMaxLength(80); entity.Property(x => x.Status).HasMaxLength(30); entity.Property(x => x.SafetyLevel).HasMaxLength(10); entity.Property(x => x.PayloadJson).HasColumnType("jsonb"); });
        modelBuilder.Entity<PainReport>(entity => { entity.ToTable("pain_reports", "members"); entity.HasKey(x => x.Id); entity.Property(x => x.Area).HasMaxLength(100); entity.Property(x => x.Side).HasMaxLength(20); entity.Property(x => x.Context).HasMaxLength(500); entity.Property(x => x.SafetyLevel).HasMaxLength(10); entity.Property(x => x.ReasonCode).HasMaxLength(100).HasDefaultValue("PAIN_REASON_NOT_RECORDED"); entity.HasIndex(x => new { x.MemberId, x.ReportedAt }); });
    }
}
