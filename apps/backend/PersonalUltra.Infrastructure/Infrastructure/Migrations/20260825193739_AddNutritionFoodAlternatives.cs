using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionFoodAlternatives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meal_food_alternatives",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealFoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_food_alternatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_food_alternatives_meal_foods_MealFoodId",
                        column: x => x.MealFoodId,
                        principalSchema: "nutrition",
                        principalTable: "meal_foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nutrition_template_food_alternatives",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutritionTemplateFoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_template_food_alternatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nutrition_template_food_alternatives_nutrition_template_foo~",
                        column: x => x.NutritionTemplateFoodId,
                        principalSchema: "nutrition",
                        principalTable: "nutrition_template_foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meal_food_alternatives_MealFoodId_Sequence",
                schema: "nutrition",
                table: "meal_food_alternatives",
                columns: new[] { "MealFoodId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_template_food_alternatives_NutritionTemplateFoodI~",
                schema: "nutrition",
                table: "nutrition_template_food_alternatives",
                columns: new[] { "NutritionTemplateFoodId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_food_alternatives",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "nutrition_template_food_alternatives",
                schema: "nutrition");
        }
    }
}
