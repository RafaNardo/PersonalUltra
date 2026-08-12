namespace PersonalUltra.Domain;

public sealed class Trainer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public TrainerBranding? Branding { get; set; }
    public List<TrainerStudent> Students { get; } = [];
    public List<StudentInvite> Invites { get; } = [];
    public List<TrainerMessage> Messages { get; } = [];
}

public sealed class TrainerBranding
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? PrimaryColor { get; set; }
    public string? LogoUrl { get; set; }
    public Trainer Trainer { get; set; } = null!;
}

public sealed class Student
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<TrainerStudent> Trainers { get; } = [];
    public Anamnesis? Anamnesis { get; set; }
    public List<TrainerMessage> Messages { get; } = [];
}

public sealed class TrainerStudent
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public Guid StudentId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public Student Student { get; set; } = null!;
}

public sealed class StudentInvite
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public string Token { get; set; } = null!;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
}

public sealed class Anamnesis
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string AnswersJson { get; set; } = "{}";
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Student Student { get; set; } = null!;
}

public sealed class TrainerMessage
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public Guid StudentId { get; set; }
    public string Message { get; set; } = null!;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
