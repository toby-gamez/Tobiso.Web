using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixQuestionsCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dropped the existing foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK__Questions__PostI__52593CB8",
                table: "Questions");

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

            // Restore the original constraint without CASCADE
            migrationBuilder.AddForeignKey(
                name: "FK__Questions__PostI__52593CB8",
                table: "Questions",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
