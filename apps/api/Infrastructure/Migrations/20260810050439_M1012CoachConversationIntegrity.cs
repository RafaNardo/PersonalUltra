using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SvrMethod.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1012CoachConversationIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier demo versions did not enforce one conversation per member.
            // Keep the oldest conversation and move its duplicate messages before
            // applying the uniqueness constraint.
            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id", "MemberId",
                        FIRST_VALUE("Id") OVER (PARTITION BY "MemberId" ORDER BY "CreatedAt", "Id") AS "KeptConversationId",
                        ROW_NUMBER() OVER (PARTITION BY "MemberId" ORDER BY "CreatedAt", "Id") AS "RowNumber"
                    FROM coaching.conversations
                ), duplicates AS (
                    SELECT "Id", "KeptConversationId" FROM ranked WHERE "RowNumber" > 1
                )
                UPDATE coaching.messages AS messages
                SET "ConversationId" = duplicates."KeptConversationId"
                FROM duplicates
                WHERE messages."ConversationId" = duplicates."Id";
                """);

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id", "MemberId",
                        ROW_NUMBER() OVER (PARTITION BY "MemberId" ORDER BY "CreatedAt", "Id") AS "RowNumber"
                    FROM coaching.conversations
                )
                DELETE FROM coaching.conversations AS conversation
                USING ranked
                WHERE conversation."Id" = ranked."Id" AND ranked."RowNumber" > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_messages_ConversationId",
                schema: "coaching",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_conversations_MemberId",
                schema: "coaching",
                table: "conversations");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId_CreatedAt_Id",
                schema: "coaching",
                table: "messages",
                columns: new[] { "ConversationId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_conversations_MemberId",
                schema: "coaching",
                table: "conversations",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_messages_ConversationId_CreatedAt_Id",
                schema: "coaching",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_conversations_MemberId",
                schema: "coaching",
                table: "conversations");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                schema: "coaching",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_MemberId",
                schema: "coaching",
                table: "conversations",
                column: "MemberId");
        }
    }
}
