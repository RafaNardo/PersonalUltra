using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAndSessionExerciseSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Repetitions",
                schema: "training",
                table: "workout_session_exercises",
                newName: "RepetitionsMin");

            migrationBuilder.RenameColumn(
                name: "Repetitions",
                schema: "training",
                table: "student_workout_exercises",
                newName: "RepetitionsMin");

            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExerciseId",
                schema: "training",
                table: "workout_session_exercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageRef",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMuscleGroup",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "RepetitionsMax",
                schema: "training",
                table: "workout_session_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "RestSeconds",
                schema: "training",
                table: "workout_session_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                schema: "training",
                table: "student_workout_exercises",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExerciseId",
                schema: "training",
                table: "student_workout_exercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageRef",
                schema: "training",
                table: "student_workout_exercises",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                schema: "training",
                table: "student_workout_exercises",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMuscleGroup",
                schema: "training",
                table: "student_workout_exercises",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "RepetitionsMax",
                schema: "training",
                table: "student_workout_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE training.student_workout_exercises AS snapshot
                SET "ExerciseId" = catalog."Id",
                    "PrimaryMuscleGroup" = catalog."PrimaryMuscleGroup",
                    "Equipment" = catalog."Equipment",
                    "ImageRef" = catalog."ImageRef",
                    "Instructions" = catalog."Instructions"
                FROM training.exercises AS catalog
                WHERE lower(btrim(snapshot."Name")) = lower(btrim(catalog."Name"))
                   OR (lower(btrim(snapshot."Name")) = 'supino reto' AND catalog."Slug" = 'supino-reto-com-barra');

                UPDATE training.student_workout_exercises
                SET "RepetitionsMax" = "RepetitionsMin";

                UPDATE training.workout_session_exercises AS session_exercise
                SET "ExerciseId" = workout_exercise."ExerciseId",
                    "PrimaryMuscleGroup" = workout_exercise."PrimaryMuscleGroup",
                    "Equipment" = workout_exercise."Equipment",
                    "ImageRef" = workout_exercise."ImageRef",
                    "Instructions" = workout_exercise."Instructions",
                    "RepetitionsMax" = session_exercise."RepetitionsMin",
                    "RestSeconds" = workout_exercise."RestSeconds",
                    "Notes" = workout_exercise."Notes"
                FROM training.workout_sessions AS session
                INNER JOIN training.student_workout_exercises AS workout_exercise
                    ON workout_exercise."StudentWorkoutId" = session."StudentWorkoutId"
                WHERE session."Id" = session_exercise."WorkoutSessionId"
                  AND workout_exercise."Sequence" = session_exercise."Sequence";

                UPDATE training.workout_session_exercises AS session_exercise
                SET "ExerciseId" = catalog."Id",
                    "PrimaryMuscleGroup" = catalog."PrimaryMuscleGroup",
                    "Equipment" = catalog."Equipment",
                    "ImageRef" = catalog."ImageRef",
                    "Instructions" = catalog."Instructions"
                FROM training.exercises AS catalog
                WHERE session_exercise."ExerciseId" IS NULL
                  AND (lower(btrim(session_exercise."Name")) = lower(btrim(catalog."Name"))
                    OR (lower(btrim(session_exercise."Name")) = 'supino reto' AND catalog."Slug" = 'supino-reto-com-barra'));

                UPDATE training.workout_session_exercises
                SET "RepetitionsMax" = COALESCE("RepetitionsMax", "RepetitionsMin"),
                    "RestSeconds" = COALESCE("RestSeconds", 0),
                    "Notes" = COALESCE("Notes", '');
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RepetitionsMax",
                schema: "training",
                table: "student_workout_exercises",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RepetitionsMax",
                schema: "training",
                table: "workout_session_exercises",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RestSeconds",
                schema: "training",
                table: "workout_session_exercises",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_ExerciseId",
                schema: "training",
                table: "workout_session_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_student_workout_exercises_ExerciseId",
                schema: "training",
                table: "student_workout_exercises",
                column: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_student_workout_exercises_exercises_ExerciseId",
                schema: "training",
                table: "student_workout_exercises",
                column: "ExerciseId",
                principalSchema: "training",
                principalTable: "exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_session_exercises_exercises_ExerciseId",
                schema: "training",
                table: "workout_session_exercises",
                column: "ExerciseId",
                principalSchema: "training",
                principalTable: "exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM training.student_workout_exercises
                        WHERE "RepetitionsMin" <> "RepetitionsMax"
                    ) OR EXISTS (
                        SELECT 1 FROM training.workout_session_exercises
                        WHERE "RepetitionsMin" <> "RepetitionsMax"
                    ) THEN
                        RAISE EXCEPTION 'Cannot downgrade exercise snapshots without losing repetition ranges.'
                            USING HINT = 'Make RepetitionsMin equal RepetitionsMax before retrying the downgrade.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_student_workout_exercises_exercises_ExerciseId",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_session_exercises_exercises_ExerciseId",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropIndex(
                name: "IX_workout_session_exercises_ExerciseId",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropIndex(
                name: "IX_student_workout_exercises_ExerciseId",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "Equipment",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "ImageRef",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "Instructions",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "PrimaryMuscleGroup",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "RepetitionsMax",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "RestSeconds",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "Equipment",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "ImageRef",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "Instructions",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "PrimaryMuscleGroup",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "RepetitionsMax",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.RenameColumn(
                name: "RepetitionsMin",
                schema: "training",
                table: "workout_session_exercises",
                newName: "Repetitions");

            migrationBuilder.RenameColumn(
                name: "RepetitionsMin",
                schema: "training",
                table: "student_workout_exercises",
                newName: "Repetitions");
        }
    }
}
