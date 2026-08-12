using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SvrMethod.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2A3StandardPlanProvisioningIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_plans_MemberId",
                schema: "plans",
                table: "plans",
                column: "MemberId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_foods_Name",
                schema: "nutrition",
                table: "foods",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_plans_MemberId",
                schema: "plans",
                table: "plans");

            migrationBuilder.DropIndex(
                name: "IX_foods_Name",
                schema: "nutrition",
                table: "foods");
        }
    }
}
