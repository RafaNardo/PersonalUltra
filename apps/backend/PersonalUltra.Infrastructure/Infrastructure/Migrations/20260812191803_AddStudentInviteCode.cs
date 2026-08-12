using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                schema: "core",
                table: "student_invites",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_invites_InviteCode",
                schema: "core",
                table: "student_invites",
                column: "InviteCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_student_invites_InviteCode",
                schema: "core",
                table: "student_invites");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                schema: "core",
                table: "student_invites");
        }
    }
}
