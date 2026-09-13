using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClerkUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClerkUserId",
                schema: "core",
                table: "trainers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClerkUserId",
                schema: "core",
                table: "students",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainers_ClerkUserId",
                schema: "core",
                table: "trainers",
                column: "ClerkUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_ClerkUserId",
                schema: "core",
                table: "students",
                column: "ClerkUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trainers_ClerkUserId",
                schema: "core",
                table: "trainers");

            migrationBuilder.DropIndex(
                name: "IX_students_ClerkUserId",
                schema: "core",
                table: "students");

            migrationBuilder.DropColumn(
                name: "ClerkUserId",
                schema: "core",
                table: "trainers");

            migrationBuilder.DropColumn(
                name: "ClerkUserId",
                schema: "core",
                table: "students");
        }
    }
}
