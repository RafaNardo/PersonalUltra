using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

public sealed class PersonalUltraDbContext(DbContextOptions<PersonalUltraDbContext> options) : DbContext(options)
{
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<TrainerBranding> TrainerBrandings => Set<TrainerBranding>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<TrainerStudent> TrainerStudents => Set<TrainerStudent>();
    public DbSet<StudentInvite> StudentInvites => Set<StudentInvite>();
    public DbSet<Anamnesis> Anamneses => Set<Anamnesis>();
    public DbSet<TrainerMessage> TrainerMessages => Set<TrainerMessage>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<WorkoutTemplateExercise> WorkoutTemplateExercises => Set<WorkoutTemplateExercise>();
    public DbSet<StudentWorkout> StudentWorkouts => Set<StudentWorkout>();
    public DbSet<StudentWorkoutExercise> StudentWorkoutExercises => Set<StudentWorkoutExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<SetPerformance> SetPerformances => Set<SetPerformance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<Trainer>(entity => { entity.ToTable("trainers", "core"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(200); });
        modelBuilder.Entity<TrainerBranding>(entity => { entity.ToTable("trainer_brandings", "core"); entity.HasKey(x => x.Id); entity.Property(x => x.DisplayName).HasMaxLength(200); entity.Property(x => x.PrimaryColor).HasMaxLength(20); entity.Property(x => x.LogoUrl).HasMaxLength(2000); entity.HasIndex(x => x.TrainerId).IsUnique(); entity.HasOne(x => x.Trainer).WithOne(x => x.Branding).HasForeignKey<TrainerBranding>(x => x.TrainerId); });
        modelBuilder.Entity<Student>(entity => { entity.ToTable("students", "core"); entity.HasKey(x => x.Id); entity.Property(x => x.FirstName).HasMaxLength(100); entity.Property(x => x.LastName).HasMaxLength(100); entity.Property(x => x.Email).HasMaxLength(320); entity.Property(x => x.Phone).HasMaxLength(16); entity.HasIndex(x => x.Email).IsUnique(); });
        modelBuilder.Entity<TrainerStudent>(entity => { entity.ToTable("trainer_students", "core"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.TrainerId, x.StudentId }).IsUnique(); entity.HasOne(x => x.Trainer).WithMany(x => x.Students).HasForeignKey(x => x.TrainerId); entity.HasOne(x => x.Student).WithMany(x => x.Trainers).HasForeignKey(x => x.StudentId); });
        modelBuilder.Entity<StudentInvite>(entity => { entity.ToTable("student_invites", "core"); entity.HasKey(x => x.Id); entity.Property(x => x.Token).HasMaxLength(200); entity.Property(x => x.InviteCode).HasMaxLength(6); entity.Property(x => x.Email).HasMaxLength(320); entity.HasIndex(x => x.Token).IsUnique(); entity.HasIndex(x => x.InviteCode).IsUnique(); entity.HasOne(x => x.Trainer).WithMany(x => x.Invites).HasForeignKey(x => x.TrainerId); });
        modelBuilder.Entity<Anamnesis>(entity => { entity.ToTable("anamneses", "core"); entity.HasKey(x => x.Id); entity.Property(x => x.AnswersJson).HasColumnType("jsonb"); entity.HasIndex(x => x.StudentId).IsUnique(); entity.HasOne(x => x.Student).WithOne(x => x.Anamnesis).HasForeignKey<Anamnesis>(x => x.StudentId); });
        modelBuilder.Entity<TrainerMessage>(entity => { entity.ToTable("trainer_messages", "engagement"); entity.HasKey(x => x.Id); entity.Property(x => x.Message).HasMaxLength(1000); entity.HasOne(x => x.Trainer).WithMany(x => x.Messages).HasForeignKey(x => x.TrainerId); entity.HasOne(x => x.Student).WithMany(x => x.Messages).HasForeignKey(x => x.StudentId); entity.HasIndex(x => new { x.StudentId, x.StartsAt }); });
        modelBuilder.Entity<WorkoutTemplate>(entity => { entity.ToTable("workout_templates", "training"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.Notes).HasMaxLength(2000); entity.HasOne(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId); });
        modelBuilder.Entity<WorkoutTemplateExercise>(entity => { entity.ToTable("workout_template_exercises", "training"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.Notes).HasMaxLength(1000); entity.HasIndex(x => new { x.WorkoutTemplateId, x.Sequence }).IsUnique(); entity.HasOne(x => x.WorkoutTemplate).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutTemplateId); });
        modelBuilder.Entity<StudentWorkout>(entity => { entity.ToTable("student_workouts", "training"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.Notes).HasMaxLength(2000); entity.HasOne(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId); entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId); });
        modelBuilder.Entity<StudentWorkoutExercise>(entity => { entity.ToTable("student_workout_exercises", "training"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(200); entity.Property(x => x.Notes).HasMaxLength(1000); entity.HasIndex(x => new { x.StudentWorkoutId, x.Sequence }).IsUnique(); entity.HasOne(x => x.StudentWorkout).WithMany(x => x.Exercises).HasForeignKey(x => x.StudentWorkoutId); });
        modelBuilder.Entity<WorkoutSession>(entity => { entity.ToTable("workout_sessions", "training"); entity.HasKey(x => x.Id); entity.Property(x => x.Status).HasMaxLength(32); entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId); entity.HasOne(x => x.StudentWorkout).WithMany(x => x.Sessions).HasForeignKey(x => x.StudentWorkoutId); });
        modelBuilder.Entity<WorkoutSessionExercise>(entity => { entity.ToTable("workout_session_exercises", "training"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(200); entity.HasOne(x => x.WorkoutSession).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutSessionId); });
        modelBuilder.Entity<SetPerformance>(entity => { entity.ToTable("set_performances", "training"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.WorkoutSessionExerciseId, x.SetNumber }).IsUnique(); entity.HasOne(x => x.WorkoutSessionExercise).WithMany(x => x.Performances).HasForeignKey(x => x.WorkoutSessionExerciseId); });
    }
}
