using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nutrition_templates",
                schema: "nutrition",
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
                    table.PrimaryKey("PK_nutrition_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nutrition_templates_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nutrition_template_meals",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutritionTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_template_meals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nutrition_template_meals_nutrition_templates_NutritionTempl~",
                        column: x => x.NutritionTemplateId,
                        principalSchema: "nutrition",
                        principalTable: "nutrition_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nutrition_template_foods",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutritionTemplateMealId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_template_foods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nutrition_template_foods_nutrition_template_meals_Nutrition~",
                        column: x => x.NutritionTemplateMealId,
                        principalSchema: "nutrition",
                        principalTable: "nutrition_template_meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_template_foods_NutritionTemplateMealId_Sequence",
                schema: "nutrition",
                table: "nutrition_template_foods",
                columns: new[] { "NutritionTemplateMealId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_template_meals_NutritionTemplateId_Sequence",
                schema: "nutrition",
                table: "nutrition_template_meals",
                columns: new[] { "NutritionTemplateId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_templates_TrainerId_Name",
                schema: "nutrition",
                table: "nutrition_templates",
                columns: new[] { "TrainerId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nutrition_template_foods",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "nutrition_template_meals",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "nutrition_templates",
                schema: "nutrition");
        }
    }
}
