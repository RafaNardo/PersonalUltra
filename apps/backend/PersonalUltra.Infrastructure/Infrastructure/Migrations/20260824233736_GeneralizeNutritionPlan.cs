using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeNutritionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_meal_foods_MealId",
                schema: "nutrition",
                table: "meal_foods");

            migrationBuilder.RenameColumn(
                name: "QuantityGrams",
                schema: "nutrition",
                table: "meal_foods",
                newName: "Quantity");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "nutrition",
                table: "meal_foods",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                schema: "nutrition",
                table: "meal_foods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                schema: "nutrition",
                table: "meal_foods",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE nutrition.nutrition_plans
                SET "CreatedAt" = "UpdatedAt",
                    "CreatedByTrainerId" = "TrainerId",
                    "UpdatedByTrainerId" = "TrainerId";

                UPDATE nutrition.meal_foods
                SET "Unit" = 'g';

                WITH sequenced AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "MealId" ORDER BY "Id") AS sequence
                    FROM nutrition.meal_foods
                )
                UPDATE nutrition.meal_foods AS food
                SET "Sequence" = sequenced.sequence
                FROM sequenced
                WHERE food."Id" = sequenced."Id";
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Sequence",
                schema: "nutrition",
                table: "meal_foods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                schema: "nutrition",
                table: "meal_foods",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "CreatedByTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_TrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "UpdatedByTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_foods_MealId_Sequence",
                schema: "nutrition",
                table: "meal_foods",
                columns: new[] { "MealId", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_plans_trainers_CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "CreatedByTrainerId",
                principalSchema: "core",
                principalTable: "trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_plans_trainers_TrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "TrainerId",
                principalSchema: "core",
                principalTable: "trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_plans_trainers_UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "UpdatedByTrainerId",
                principalSchema: "core",
                principalTable: "trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_plans_trainers_CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_plans_trainers_TrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_plans_trainers_UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropIndex(
                name: "IX_nutrition_plans_CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropIndex(
                name: "IX_nutrition_plans_TrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropIndex(
                name: "IX_nutrition_plans_UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropIndex(
                name: "IX_meal_foods_MealId_Sequence",
                schema: "nutrition",
                table: "meal_foods");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropColumn(
                name: "CreatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropColumn(
                name: "UpdatedByTrainerId",
                schema: "nutrition",
                table: "nutrition_plans");

            migrationBuilder.DropColumn(
                name: "Sequence",
                schema: "nutrition",
                table: "meal_foods");

            migrationBuilder.DropColumn(
                name: "Unit",
                schema: "nutrition",
                table: "meal_foods");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "nutrition",
                table: "meal_foods",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.RenameColumn(
                name: "Quantity",
                schema: "nutrition",
                table: "meal_foods",
                newName: "QuantityGrams");

            migrationBuilder.CreateIndex(
                name: "IX_meal_foods_MealId",
                schema: "nutrition",
                table: "meal_foods",
                column: "MealId");
        }
    }
}
