using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseTrackingAndConfirmedCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetDurationSeconds",
                schema: "training",
                table: "workout_template_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingMode",
                schema: "training",
                table: "workout_template_exercises",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Repetitions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedCompletedAt",
                schema: "training",
                table: "workout_session_exercises",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetDurationSeconds",
                schema: "training",
                table: "workout_session_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingMode",
                schema: "training",
                table: "workout_session_exercises",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Repetitions");

            migrationBuilder.AddColumn<int>(
                name: "TargetDurationSeconds",
                schema: "training",
                table: "student_workout_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingMode",
                schema: "training",
                table: "student_workout_exercises",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Repetitions");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightKg",
                schema: "training",
                table: "set_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Repetitions",
                schema: "training",
                table: "set_performances",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                schema: "training",
                table: "set_performances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultDurationSeconds",
                schema: "training",
                table: "exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTrackingMode",
                schema: "training",
                table: "exercises",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Repetitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetDurationSeconds",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.DropColumn(
                name: "TrackingMode",
                schema: "training",
                table: "workout_template_exercises");

            migrationBuilder.DropColumn(
                name: "ConfirmedCompletedAt",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "TargetDurationSeconds",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "TrackingMode",
                schema: "training",
                table: "workout_session_exercises");

            migrationBuilder.DropColumn(
                name: "TargetDurationSeconds",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "TrackingMode",
                schema: "training",
                table: "student_workout_exercises");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                schema: "training",
                table: "set_performances");

            migrationBuilder.DropColumn(
                name: "DefaultDurationSeconds",
                schema: "training",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "DefaultTrackingMode",
                schema: "training",
                table: "exercises");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightKg",
                schema: "training",
                table: "set_performances",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Repetitions",
                schema: "training",
                table: "set_performances",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
