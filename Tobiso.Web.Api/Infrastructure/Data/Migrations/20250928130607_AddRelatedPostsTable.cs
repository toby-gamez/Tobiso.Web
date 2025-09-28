using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedPostsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelatedPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    RelatedPostId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedPosts", x => x.Id);
                    table.CheckConstraint("CK_RelatedPost_DifferentPosts", "[PostId] <> [RelatedPostId]");
                    table.ForeignKey(
                        name: "FK_RelatedPosts_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelatedPosts_Posts_RelatedPostId",
                        column: x => x.RelatedPostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPosts_PostId_RelatedPostId",
                table: "RelatedPosts",
                columns: new[] { "PostId", "RelatedPostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPosts_RelatedPostId",
                table: "RelatedPosts",
                column: "RelatedPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelatedPosts");
        }
    }
}
