using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteForQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing foreign key constraint if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__Questions__PostI__52593CB8]') AND parent_object_id = OBJECT_ID(N'[dbo].[Questions]'))
                BEGIN
                    ALTER TABLE [Questions] DROP CONSTRAINT [FK__Questions__PostI__52593CB8]
                END");

            // Add the foreign key constraint with CASCADE delete
            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Posts_PostId",
                table: "Questions",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the CASCADE foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Posts_PostId",
                table: "Questions");

            // Restore the original constraint without CASCADE (if needed)
            migrationBuilder.AddForeignKey(
                name: "FK__Questions__PostI__52593CB8",
                table: "Questions",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
