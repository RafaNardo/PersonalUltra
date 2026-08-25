using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionDailyGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyCalories",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "numeric(7,0)",
                precision: 7,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyCarbohydratesGrams",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "numeric(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyFatGrams",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "numeric(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyProteinGrams",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "numeric(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyCalories",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropColumn(
                name: "DailyCarbohydratesGrams",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropColumn(
                name: "DailyFatGrams",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropColumn(
                name: "DailyProteinGrams",
                schema: "nutrition",
                table: "nutrition_plans");
        }
    }
}
