using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentWorkoutSuggestedOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SuggestedOrder",
                schema: "training",
                table: "student_workouts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH ranked_workouts AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "StudentId"
                            ORDER BY "RecommendedDay", "CreatedAt", "Id"
                        )::integer AS "SuggestedOrder"
                    FROM training.student_workouts
                )
                UPDATE training.student_workouts AS workout
                SET "SuggestedOrder" = ranked."SuggestedOrder"
                FROM ranked_workouts AS ranked
                WHERE workout."Id" = ranked."Id";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "SuggestedOrder",
                schema: "training",
                table: "student_workouts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_workouts_StudentId_SuggestedOrder",
                schema: "training",
                table: "student_workouts",
                columns: new[] { "StudentId", "SuggestedOrder" },
                unique: true,
                filter: "\"IsActive\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_student_workouts_StudentId_SuggestedOrder",
                schema: "training",
                table: "student_workouts");

            migrationBuilder.DropColumn(
                name: "SuggestedOrder",
                schema: "training",
                table: "student_workouts");
        }
    }
}
