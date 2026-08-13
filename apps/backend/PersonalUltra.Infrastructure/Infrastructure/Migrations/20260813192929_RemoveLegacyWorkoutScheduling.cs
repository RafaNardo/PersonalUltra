using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyWorkoutScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecommended",
                schema: "training",
                table: "student_workouts");

            migrationBuilder.DropColumn(
                name: "RecommendedDay",
                schema: "training",
                table: "student_workouts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                schema: "training",
                table: "student_workouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecommendedDay",
                schema: "training",
                table: "student_workouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
