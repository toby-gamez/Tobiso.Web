using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Grades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create Grades and PostVersions first, then migrate existing post content into PostVersions

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastFix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEdit = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostVersions_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PostVersions_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Level",
                table: "Grades",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_GradeId",
                table: "PostVersions",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_PostId",
                table: "PostVersions",
                column: "PostId");

            // Seed default grades (6-9)
            migrationBuilder.InsertData(
                table: "Grades",
                columns: new[] { "Name", "Level" },
                values: new object[,] {
                    { "6. třída", 6 },
                    { "7. třída", 7 },
                    { "8. třída", 8 },
                    { "9. třída", 9 }
                }
            );

            // Migrate existing Posts content into PostVersions with Grade = 9
            migrationBuilder.Sql(@"
                INSERT INTO PostVersions (PostId, GradeId, Content, LastFix, LastEdit)
                SELECT Id, (SELECT TOP 1 Id FROM Grades WHERE Level = 9), Content, LastFix, LastEdit FROM Posts
            ");

            // Now drop legacy columns from Posts (content moved to PostVersions)
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LastEdit",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LastFix",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back legacy columns on Posts
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEdit",
                table: "Posts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFix",
                table: "Posts",
                type: "datetime2",
                nullable: true);

            // Restore content from PostVersions (take highest-level version per post)
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Content = pv.Content, p.LastFix = pv.LastFix, p.LastEdit = pv.LastEdit
                FROM Posts p
                INNER JOIN (
                    SELECT PostId, Content, LastFix, LastEdit,
                        ROW_NUMBER() OVER (PARTITION BY PostId ORDER BY GradeId DESC) as rn
                    FROM PostVersions
                ) pv ON pv.PostId = p.Id AND pv.rn = 1
            ");

            // Drop PostVersions and Grades
            migrationBuilder.DropTable(
                name: "PostVersions");

            migrationBuilder.DropTable(
                name: "Grades");
        }
    }
}
