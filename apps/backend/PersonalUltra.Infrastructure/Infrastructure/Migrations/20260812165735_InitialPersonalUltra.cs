using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersonalUltra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "coaching");

            migrationBuilder.EnsureSchema(
                name: "nutrition");

            migrationBuilder.EnsureSchema(
                name: "training");

            migrationBuilder.EnsureSchema(
                name: "members");

            migrationBuilder.EnsureSchema(
                name: "plans");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "progress");

            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryMuscleGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "foods",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CaloriesPer100g = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    ProteinPer100g = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CarbsPer100g = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    FatPer100g = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_foods", x => x.Id);
                    table.CheckConstraint("CK_foods_nutrition_nonnegative", "\"CaloriesPer100g\" > 0 AND \"ProteinPer100g\" >= 0 AND \"CarbsPer100g\" >= 0 AND \"FatPer100g\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "pain_reports",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Side = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    Context = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SafetyLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "PAIN_REASON_NOT_RECORDED"),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pain_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trainers",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "anamneses",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswersJson = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anamneses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_anamneses_students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "core",
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_invites",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_invites_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_brandings",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_brandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trainer_brandings_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_students",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trainer_students_students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "core",
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_students_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OnboardingCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_members_users_AuthUserId",
                        column: x => x.AuthUserId,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                schema: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plans_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Goal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrainingDaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    SessionDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    TrainingLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EquipmentNotes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    HeightCm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    HealthConditions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    MovementRestrictions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    CurrentPainDescription = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    NutritionPreferences = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    NutritionRestrictions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profiles_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weight_entries",
                schema: "progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weight_entries_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "coaching",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nutrition_plans",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaloriesTarget = table.Column<int>(type: "integer", nullable: false),
                    ProteinGramsTarget = table.Column<int>(type: "integer", nullable: false),
                    CarbsGramsTarget = table.Column<int>(type: "integer", nullable: false),
                    FatGramsTarget = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_plans", x => x.Id);
                    table.CheckConstraint("CK_nutrition_plans_targets_positive", "\"CaloriesTarget\" > 0 AND \"ProteinGramsTarget\" > 0 AND \"CarbsGramsTarget\" >= 0 AND \"FatGramsTarget\" >= 0");
                    table.ForeignKey(
                        name: "FK_nutrition_plans_plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "plans",
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_plans",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionsPerWeek = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_training_plans_plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "plans",
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_templates",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutritionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_templates", x => x.Id);
                    table.CheckConstraint("CK_meal_templates_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_meal_templates_nutrition_plans_NutritionPlanId",
                        column: x => x.NutritionPlanId,
                        principalSchema: "nutrition",
                        principalTable: "nutrition_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_templates",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_templates_training_plans_TrainingPlanId",
                        column: x => x.TrainingPlanId,
                        principalSchema: "training",
                        principalTable: "training_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_logs",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MealTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_logs_meal_templates_MealTemplateId",
                        column: x => x.MealTemplateId,
                        principalSchema: "nutrition",
                        principalTable: "meal_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_logs_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_template_foods",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityGrams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_template_foods", x => x.Id);
                    table.CheckConstraint("CK_meal_template_foods_quantity_positive", "\"QuantityGrams\" > 0");
                    table.ForeignKey(
                        name: "FK_meal_template_foods_foods_FoodId",
                        column: x => x.FoodId,
                        principalSchema: "nutrition",
                        principalTable: "foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_template_foods_meal_templates_MealTemplateId",
                        column: x => x.MealTemplateId,
                        principalSchema: "nutrition",
                        principalTable: "meal_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledFor = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_sessions_workout_templates_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalSchema: "training",
                        principalTable: "workout_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_template_exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PrescribedSets = table.Column<int>(type: "integer", nullable: false),
                    MinimumRepetitions = table.Column<int>(type: "integer", nullable: false),
                    MaximumRepetitions = table.Column<int>(type: "integer", nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_template_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_template_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalSchema: "training",
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_template_exercises_workout_templates_WorkoutTemplat~",
                        column: x => x.WorkoutTemplateId,
                        principalSchema: "training",
                        principalTable: "workout_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PrescribedSets = table.Column<int>(type: "integer", nullable: false),
                    MinimumRepetitions = table.Column<int>(type: "integer", nullable: false),
                    MaximumRepetitions = table.Column<int>(type: "integer", nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false),
                    ExerciseSnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalSchema: "training",
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_workout_sessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalSchema: "training",
                        principalTable: "workout_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "set_performances",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    RepsInReserve = table.Column<int>(type: "integer", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_set_performances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_set_performances_workout_session_exercises_WorkoutSessionEx~",
                        column: x => x.WorkoutSessionExerciseId,
                        principalSchema: "training",
                        principalTable: "workout_session_exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anamneses_StudentId",
                schema: "core",
                table: "anamneses",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_MemberId",
                schema: "coaching",
                table: "conversations",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_logs_MealTemplateId",
                schema: "nutrition",
                table: "daily_logs",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_logs_MemberId_Date_MealTemplateId",
                schema: "nutrition",
                table: "daily_logs",
                columns: new[] { "MemberId", "Date", "MealTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Name",
                schema: "training",
                table: "exercises",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_foods_Name",
                schema: "nutrition",
                table: "foods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_template_foods_FoodId",
                schema: "nutrition",
                table: "meal_template_foods",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_template_foods_MealTemplateId",
                schema: "nutrition",
                table: "meal_template_foods",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_templates_NutritionPlanId_Sequence",
                schema: "nutrition",
                table: "meal_templates",
                columns: new[] { "NutritionPlanId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_AuthUserId",
                schema: "members",
                table: "members",
                column: "AuthUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId_CreatedAt_Id",
                schema: "coaching",
                table: "messages",
                columns: new[] { "ConversationId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_PlanId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "PlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pain_reports_MemberId_ReportedAt",
                schema: "members",
                table: "pain_reports",
                columns: new[] { "MemberId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_plans_MemberId",
                schema: "plans",
                table: "plans",
                column: "MemberId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_plans_MemberId_Status",
                schema: "plans",
                table: "plans",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_profiles_MemberId",
                schema: "members",
                table: "profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_set_performances_WorkoutSessionExerciseId_ClientOperationId",
                schema: "training",
                table: "set_performances",
                columns: new[] { "WorkoutSessionExerciseId", "ClientOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_set_performances_WorkoutSessionExerciseId_SetNumber",
                schema: "training",
                table: "set_performances",
                columns: new[] { "WorkoutSessionExerciseId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_invites_Token",
                schema: "core",
                table: "student_invites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_invites_TrainerId",
                schema: "core",
                table: "student_invites",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_students_Email",
                schema: "core",
                table: "students",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_brandings_TrainerId",
                schema: "core",
                table: "trainer_brandings",
                column: "TrainerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_students_StudentId",
                schema: "core",
                table: "trainer_students",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_students_TrainerId_StudentId",
                schema: "core",
                table: "trainer_students",
                columns: new[] { "TrainerId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_plans_PlanId",
                schema: "training",
                table: "training_plans",
                column: "PlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "auth",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weight_entries_MemberId_RecordedAt",
                schema: "progress",
                table: "weight_entries",
                columns: new[] { "MemberId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_ExerciseId",
                schema: "training",
                table: "workout_session_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_WorkoutSessionId_Sequence",
                schema: "training",
                table: "workout_session_exercises",
                columns: new[] { "WorkoutSessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_MemberId_ScheduledFor",
                schema: "training",
                table: "workout_sessions",
                columns: new[] { "MemberId", "ScheduledFor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_WorkoutTemplateId",
                schema: "training",
                table: "workout_sessions",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_template_exercises_ExerciseId",
                schema: "training",
                table: "workout_template_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_template_exercises_WorkoutTemplateId_Sequence",
                schema: "training",
                table: "workout_template_exercises",
                columns: new[] { "WorkoutTemplateId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_templates_TrainingPlanId",
                schema: "training",
                table: "workout_templates",
                column: "TrainingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anamneses",
                schema: "core");

            migrationBuilder.DropTable(
                name: "daily_logs",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_template_foods",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "pain_reports",
                schema: "members");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "members");

            migrationBuilder.DropTable(
                name: "set_performances",
                schema: "training");

            migrationBuilder.DropTable(
                name: "student_invites",
                schema: "core");

            migrationBuilder.DropTable(
                name: "trainer_brandings",
                schema: "core");

            migrationBuilder.DropTable(
                name: "trainer_students",
                schema: "core");

            migrationBuilder.DropTable(
                name: "weight_entries",
                schema: "progress");

            migrationBuilder.DropTable(
                name: "workout_template_exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "foods",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_templates",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "workout_session_exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "students",
                schema: "core");

            migrationBuilder.DropTable(
                name: "trainers",
                schema: "core");

            migrationBuilder.DropTable(
                name: "nutrition_plans",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_sessions",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_templates",
                schema: "training");

            migrationBuilder.DropTable(
                name: "training_plans",
                schema: "training");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "members",
                schema: "members");

            migrationBuilder.DropTable(
                name: "users",
                schema: "auth");
        }
    }
}
