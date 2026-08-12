using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalUltra.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "engagement");

            migrationBuilder.CreateTable(
                name: "trainer_messages",
                schema: "engagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trainer_messages_students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "core",
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_messages_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalSchema: "core",
                        principalTable: "trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_messages_StudentId_StartsAt",
                schema: "engagement",
                table: "trainer_messages",
                columns: new[] { "StudentId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_messages_TrainerId",
                schema: "engagement",
                table: "trainer_messages",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trainer_messages",
                schema: "engagement");
        }
    }
}
