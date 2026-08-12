using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1006NutritionDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_nutrition_plans_PlanId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropIndex(
                name: "IX_meal_templates_NutritionPlanId",
                schema: "nutrition",
                table: "meal_templates");

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_PlanId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "PlanId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_nutrition_plans_targets_positive",
                schema: "nutrition",
                table: "nutrition_plans",
                sql: "\"CaloriesTarget\" > 0 AND \"ProteinGramsTarget\" > 0 AND \"CarbsGramsTarget\" >= 0 AND \"FatGramsTarget\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_meal_templates_NutritionPlanId_Sequence",
                schema: "nutrition",
                table: "meal_templates",
                columns: new[] { "NutritionPlanId", "Sequence" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_meal_templates_sequence_positive",
                schema: "nutrition",
                table: "meal_templates",
                sql: "\"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_meal_template_foods_quantity_positive",
                schema: "nutrition",
                table: "meal_template_foods",
                sql: "\"QuantityGrams\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_foods_nutrition_nonnegative",
                schema: "nutrition",
                table: "foods",
                sql: "\"CaloriesPer100g\" > 0 AND \"ProteinPer100g\" >= 0 AND \"CarbsPer100g\" >= 0 AND \"FatPer100g\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_nutrition_plans_PlanId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_nutrition_plans_targets_positive",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropIndex(
                name: "IX_meal_templates_NutritionPlanId_Sequence",
                schema: "nutrition",
                table: "meal_templates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_meal_templates_sequence_positive",
                schema: "nutrition",
                table: "meal_templates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_meal_template_foods_quantity_positive",
                schema: "nutrition",
                table: "meal_template_foods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_foods_nutrition_nonnegative",
                schema: "nutrition",
                table: "foods");

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_PlanId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_templates_NutritionPlanId",
                schema: "nutrition",
                table: "meal_templates",
                column: "NutritionPlanId");
        }
    }
}
