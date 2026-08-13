using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorWorkoutTemplateExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Repetitions",
                schema: "training",
                table: "workout_template_exercises",
                newName: "RepetitionsMin");

            migrationBuilder.AddColumn<Guid?>(
                name: "ExerciseId",
                schema: "training",
                table: "workout_template_exercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "RepetitionsMax",
                schema: "training",
                table: "workout_template_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    ambiguous_names text;
                BEGIN
                    SELECT string_agg(candidate.normalized_name, ', ' ORDER BY candidate.normalized_name)
                    INTO ambiguous_names
                    FROM (
                        SELECT lower(btrim(exercise."Name")) AS normalized_name
                        FROM training.exercises AS exercise
                        INNER JOIN (
                            SELECT DISTINCT lower(btrim(legacy."Name")) AS normalized_name
                            FROM training.workout_template_exercises AS legacy
                        ) AS used_name ON used_name.normalized_name = lower(btrim(exercise."Name"))
                        GROUP BY lower(btrim(exercise."Name"))
                        HAVING count(*) > 1
                    ) AS candidate;

                    IF ambiguous_names IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot migrate workout template exercises because catalog names are ambiguous.'
                            USING DETAIL = 'Ambiguous normalized names: ' || ambiguous_names,
                                  HINT = 'Make each referenced catalog exercise name unique before retrying the migration.';
                    END IF;
                END $$;

                UPDATE training.workout_template_exercises AS legacy
                SET "ExerciseId" = exercise."Id",
                    "RepetitionsMax" = legacy."RepetitionsMin"
                FROM training.exercises AS exercise
                WHERE lower(btrim(legacy."Name")) = lower(btrim(exercise."Name"));

                DO $$
                DECLARE
                    unresolved_names text;
                BEGIN
                    SELECT string_agg(legacy."Name", ', ' ORDER BY legacy."Name")
                    INTO unresolved_names
                    FROM training.workout_template_exercises AS legacy
                    WHERE legacy."ExerciseId" IS NULL;

                    IF unresolved_names IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot migrate free-text workout template exercises without a catalog match.'
                            USING DETAIL = 'Unresolved exercise names: ' || unresolved_names,
                                  HINT = 'Add or rename seeded catalog entries so every legacy name has one exact case-insensitive match, then retry.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ExerciseId",
                schema: "training",
                table: "workout_template_exercises",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RepetitionsMax",
                schema: "training",
                table: "workout_template_exercises",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.CreateIndex(
                name: "IX_workout_template_exercises_ExerciseId",
                schema: "training",
                table: "workout_template_exercises",
                column: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_workout_template_exercises_exercises_ExerciseId",
                schema: "training",
                table: "workout_template_exercises",
                column: "ExerciseId",
                principalSchema: "training",
                principalTable: "exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "training",
                table: "workout_template_exercises",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM training.workout_template_exercises
                        WHERE "RepetitionsMin" <> "RepetitionsMax"
                    ) THEN
                        RAISE EXCEPTION 'Cannot downgrade workout template repetition ranges without losing data.'
                            USING HINT = 'Make RepetitionsMin equal RepetitionsMax before retrying the downgrade.';
                    END IF;
                END $$;

                UPDATE training.workout_template_exercises AS prescription
                SET "Name" = exercise."Name"
                FROM training.exercises AS exercise
                WHERE prescription."ExerciseId" = exercise."Id";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_workout_template_exercises_exercises_ExerciseId",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.DropIndex(
                name: "IX_workout_template_exercises_ExerciseId",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.DropColumn(
                name: "RepetitionsMax",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.RenameColumn(
                name: "RepetitionsMin",
                schema: "training",
                table: "workout_template_exercises",
                newName: "Repetitions");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "training",
                table: "workout_template_exercises",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
