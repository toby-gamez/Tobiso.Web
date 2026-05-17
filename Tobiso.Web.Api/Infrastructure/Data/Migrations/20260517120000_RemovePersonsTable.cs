using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    public partial class RemovePersonsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop person-related tables if they exist. Use T-SQL to be tolerant to current DB state.
            migrationBuilder.Sql("IF OBJECT_ID('dbo.PostPersonMentions', 'U') IS NOT NULL DROP TABLE [dbo].[PostPersonMentions];");
            migrationBuilder.Sql("IF OBJECT_ID('dbo.Persons', 'U') IS NOT NULL DROP TABLE [dbo].[Persons];");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate minimal Persons table schema (best-effort fallback)
            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BirthYear = table.Column<int>(type: "int", nullable: true),
                    DeathYear = table.Column<int>(type: "int", nullable: true),
                    ExternalLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            // Recreate join table PostPersonMentions (minimal schema)
            migrationBuilder.CreateTable(
                name: "PostPersonMentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostPersonMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostPersonMentions_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostPersonMentions_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostPersonMentions_PostId",
                table: "PostPersonMentions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostPersonMentions_PersonId",
                table: "PostPersonMentions",
                column: "PersonId");
        }
    }
}
