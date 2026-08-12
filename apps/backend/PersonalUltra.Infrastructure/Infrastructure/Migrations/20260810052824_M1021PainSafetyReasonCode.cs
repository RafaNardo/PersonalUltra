using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1021PainSafetyReasonCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                schema: "members",
                table: "pain_reports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "PAIN_REASON_NOT_RECORDED");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasonCode",
                schema: "members",
                table: "pain_reports");
        }
    }
}
