using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPrescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "training");

            migrationBuilder.CreateTable(
                name: "student_workouts",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecommendedDay = table.Column<int>(type: "integer", nullable: false),
                    IsRecommended = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_workouts_students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "core",
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_workouts_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_templates",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_templates_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_workout_exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_workout_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_workout_exercises_student_workouts_StudentWorkoutId",
                        column: x => x.StudentWorkoutId,
                        principalSchema: "training",
                        principalTable: "student_workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_student_workouts_StudentWorkoutId",
                        column: x => x.StudentWorkoutId,
                        principalSchema: "training",
                        principalTable: "student_workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_sessions_students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "core",
                        principalTable: "students",
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
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_template_exercises", x => x.Id);
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
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    CompletedSets = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_exercises", x => x.Id);
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
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_set_performances_WorkoutSessionExerciseId_SetNumber",
                schema: "training",
                table: "set_performances",
                columns: new[] { "WorkoutSessionExerciseId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_workout_exercises_StudentWorkoutId_Sequence",
                schema: "training",
                table: "student_workout_exercises",
                columns: new[] { "StudentWorkoutId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_workouts_StudentId",
                schema: "training",
                table: "student_workouts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_workouts_TrainerId",
                schema: "training",
                table: "student_workouts",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_WorkoutSessionId",
                schema: "training",
                table: "workout_session_exercises",
                column: "WorkoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_StudentId",
                schema: "training",
                table: "workout_sessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_StudentWorkoutId",
                schema: "training",
                table: "workout_sessions",
                column: "StudentWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_template_exercises_WorkoutTemplateId_Sequence",
                schema: "training",
                table: "workout_template_exercises",
                columns: new[] { "WorkoutTemplateId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_templates_TrainerId",
                schema: "training",
                table: "workout_templates",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "set_performances",
                schema: "training");

            migrationBuilder.DropTable(
                name: "student_workout_exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_template_exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_session_exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_templates",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_sessions",
                schema: "training");

            migrationBuilder.DropTable(
                name: "student_workouts",
                schema: "training");
        }
    }
}
