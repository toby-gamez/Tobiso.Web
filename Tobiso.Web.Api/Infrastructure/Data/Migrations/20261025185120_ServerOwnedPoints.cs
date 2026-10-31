using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ServerOwnedPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "UserGameStates");

            migrationBuilder.AddColumn<int>(
                name: "PointsBalance",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PointsLedger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsLedger_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointsLedger_UserId_CreatedAt",
                table: "PointsLedger",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsLedger_UserId_Kind_Ref",
                table: "PointsLedger",
                columns: new[] { "UserId", "Kind", "Ref" },
                unique: true,
                filter: "[Ref] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsLedger");

            migrationBuilder.DropColumn(
                name: "PointsBalance",
                table: "Users");

            migrationBuilder.AddColumn<double>(
                name: "Points",
                table: "UserGameStates",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
