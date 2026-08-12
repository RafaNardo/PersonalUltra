using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SvrMethod.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2A1MemberOnboardingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingCompletedAt",
                schema: "members",
                table: "members",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                schema: "members",
                table: "members");
        }
    }
}
