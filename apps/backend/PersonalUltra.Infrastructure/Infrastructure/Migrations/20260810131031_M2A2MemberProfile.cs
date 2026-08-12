using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2A2MemberProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Goal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrainingDaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    SessionDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    TrainingLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EquipmentNotes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    HeightCm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    HealthConditions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    MovementRestrictions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    CurrentPainDescription = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    NutritionPreferences = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    NutritionRestrictions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profiles_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profiles_MemberId",
                schema: "members",
                table: "profiles",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profiles",
                schema: "members");
        }
    }
}
