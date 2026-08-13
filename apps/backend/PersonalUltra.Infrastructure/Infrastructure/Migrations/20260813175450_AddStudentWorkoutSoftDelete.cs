using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentWorkoutSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_student_workouts_StudentId",
                schema: "training",
                table: "student_workouts");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "training",
                table: "student_workouts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_workouts_StudentId_IsActive",
                schema: "training",
                table: "student_workouts",
                columns: new[] { "StudentId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_student_workouts_StudentId_IsActive",
                schema: "training",
                table: "student_workouts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "training",
                table: "student_workouts");

            migrationBuilder.CreateIndex(
                name: "IX_student_workouts_StudentId",
                schema: "training",
                table: "student_workouts",
                column: "StudentId");
        }
    }
}
