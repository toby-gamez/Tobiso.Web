using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiChatSessionTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "AiChatSessions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            // Backfill existing sessions from their opening question so "Obecná konverzace" doesn't
            // linger for conversations that already exist, matching AiChatHistoryService.BuildTitle.
            migrationBuilder.Sql(@"
                UPDATE s
                SET s.Title = CASE
                    WHEN LEN(LTRIM(RTRIM(q.Content))) > 80
                        THEN LEFT(LTRIM(RTRIM(q.Content)), 79) + N'…'
                    ELSE LTRIM(RTRIM(q.Content))
                END
                FROM AiChatSessions s
                CROSS APPLY (
                    SELECT TOP 1 m.Content
                    FROM AiChatMessages m
                    WHERE m.SessionId = s.Id AND m.Role = 'user'
                    ORDER BY m.CreatedAt ASC
                ) q
                WHERE s.Title IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "AiChatSessions");
        }
    }
}
