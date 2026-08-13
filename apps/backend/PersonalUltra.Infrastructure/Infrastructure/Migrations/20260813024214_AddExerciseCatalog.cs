using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryMuscleGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Equipment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImageRef = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercises_IsActive_PrimaryMuscleGroup",
                schema: "training",
                table: "exercises",
                columns: new[] { "IsActive", "PrimaryMuscleGroup" });

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Slug",
                schema: "training",
                table: "exercises",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercises",
                schema: "training");
        }
    }
}
