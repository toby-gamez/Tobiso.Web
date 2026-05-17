using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tobiso.Api.Infrastructure.Data.Migrations
{
    public partial class AddGradesAndPostVersions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Level = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostVersions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(nullable: false),
                    GradeId = table.Column<int>(nullable: true),
                    Content = table.Column<string>(nullable: false),
                    LastFix = table.Column<DateTime>(nullable: true),
                    LastEdit = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostVersions_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostVersions_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_PostId",
                table: "PostVersions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_GradeId",
                table: "PostVersions",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Level",
                table: "Grades",
                column: "Level",
                unique: true);

            // Seed default grades 6-9
            migrationBuilder.InsertData("Grades", new[] { "Name", "Level" }, new object[,] {
                { "6. třída", 6 },
                { "7. třída", 7 },
                { "8. třída", 8 },
                { "9. třída", 9 }
            });

            // Migrate existing Post content into PostVersions (grade 9)
            migrationBuilder.Sql(@"
                INSERT INTO PostVersions (PostId, GradeId, Content, LastFix, LastEdit)
                SELECT Id, (SELECT Id FROM Grades WHERE Level = 9), Content, LastFix, LastEdit FROM Posts
            ");

            // Drop old columns Content, LastFix, LastEdit from Posts
            migrationBuilder.DropColumn(name: "Content", table: "Posts");
            migrationBuilder.DropColumn(name: "LastFix", table: "Posts");
            migrationBuilder.DropColumn(name: "LastEdit", table: "Posts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate columns (best-effort: use latest version)
            migrationBuilder.AddColumn<string>(name: "Content", table: "Posts", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "LastFix", table: "Posts", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "LastEdit", table: "Posts", type: "datetime2", nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Posts SET Content = pv.Content, LastFix = pv.LastFix, LastEdit = pv.LastEdit
                FROM Posts p
                JOIN (
                    SELECT PostId, Content, LastFix, LastEdit,
                        ROW_NUMBER() OVER (PARTITION BY PostId ORDER BY GradeId DESC) as rn
                    FROM PostVersions
                ) pv ON pv.PostId = p.Id AND pv.rn = 1
            ");

            migrationBuilder.DropTable(name: "PostVersions");
            migrationBuilder.DropTable(name: "Grades");
        }
    }
}
