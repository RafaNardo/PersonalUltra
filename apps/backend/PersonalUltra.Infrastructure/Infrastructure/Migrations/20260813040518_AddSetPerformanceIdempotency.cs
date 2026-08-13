using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSetPerformanceIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientOperationId",
                schema: "training",
                table: "set_performances",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_set_performances_WorkoutSessionExerciseId_ClientOperationId",
                schema: "training",
                table: "set_performances",
                columns: new[] { "WorkoutSessionExerciseId", "ClientOperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_set_performances_WorkoutSessionExerciseId_ClientOperationId",
                schema: "training",
                table: "set_performances");

            migrationBuilder.DropColumn(
                name: "ClientOperationId",
                schema: "training",
                table: "set_performances");
        }
    }
}
