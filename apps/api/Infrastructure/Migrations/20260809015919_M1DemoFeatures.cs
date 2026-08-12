using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SvrMethod.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1DemoFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "coaching");

            migrationBuilder.EnsureSchema(
                name: "nutrition");

            migrationBuilder.EnsureSchema(
                name: "progress");

            migrationBuilder.CreateTable(
                name: "actions",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    SafetyLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "foods",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CaloriesPer100g = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    ProteinPer100g = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CarbsPer100g = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    FatPer100g = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_foods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nutrition_plans",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaloriesTarget = table.Column<int>(type: "integer", nullable: false),
                    ProteinGramsTarget = table.Column<int>(type: "integer", nullable: false),
                    CarbsGramsTarget = table.Column<int>(type: "integer", nullable: false),
                    FatGramsTarget = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nutrition_plans_plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "plans",
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pain_reports",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Side = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    Context = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SafetyLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pain_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weight_entries",
                schema: "progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weight_entries_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "coaching",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_templates",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutritionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_templates_nutrition_plans_NutritionPlanId",
                        column: x => x.NutritionPlanId,
                        principalSchema: "nutrition",
                        principalTable: "nutrition_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_logs",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MealTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_logs_meal_templates_MealTemplateId",
                        column: x => x.MealTemplateId,
                        principalSchema: "nutrition",
                        principalTable: "meal_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_logs_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_template_foods",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityGrams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_template_foods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_template_foods_foods_FoodId",
                        column: x => x.FoodId,
                        principalSchema: "nutrition",
                        principalTable: "foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_template_foods_meal_templates_MealTemplateId",
                        column: x => x.MealTemplateId,
                        principalSchema: "nutrition",
                        principalTable: "meal_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversations_MemberId",
                schema: "coaching",
                table: "conversations",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_logs_MealTemplateId",
                schema: "nutrition",
                table: "daily_logs",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_logs_MemberId_Date_MealTemplateId",
                schema: "nutrition",
                table: "daily_logs",
                columns: new[] { "MemberId", "Date", "MealTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_template_foods_FoodId",
                schema: "nutrition",
                table: "meal_template_foods",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_template_foods_MealTemplateId",
                schema: "nutrition",
                table: "meal_template_foods",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_templates_NutritionPlanId",
                schema: "nutrition",
                table: "meal_templates",
                column: "NutritionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                schema: "coaching",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_plans_PlanId",
                schema: "nutrition",
                table: "nutrition_plans",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_pain_reports_MemberId_ReportedAt",
                schema: "members",
                table: "pain_reports",
                columns: new[] { "MemberId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_weight_entries_MemberId_RecordedAt",
                schema: "progress",
                table: "weight_entries",
                columns: new[] { "MemberId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actions",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "daily_logs",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_template_foods",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "pain_reports",
                schema: "members");

            migrationBuilder.DropTable(
                name: "weight_entries",
                schema: "progress");

            migrationBuilder.DropTable(
                name: "foods",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_templates",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "nutrition_plans",
                schema: "nutrition");
        }
    }
}
