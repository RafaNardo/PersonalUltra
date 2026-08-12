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
    }
}
