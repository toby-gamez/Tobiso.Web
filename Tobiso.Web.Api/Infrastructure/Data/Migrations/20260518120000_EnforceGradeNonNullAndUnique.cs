using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tobiso.Api.Infrastructure.Data.Migrations
{
    public partial class EnforceGradeNonNullAndUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure there is at least one grade (should exist from previous migration).
            // For any PostVersions with NULL GradeId, assign them to the highest-level grade.
            migrationBuilder.Sql(@"
                DECLARE @gid INT;
                SELECT TOP 1 @gid = Id FROM Grades ORDER BY Level DESC;
                UPDATE PostVersions SET GradeId = @gid WHERE GradeId IS NULL;
            ");

            // Alter column to be NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "GradeId",
                table: "PostVersions",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // Add unique constraint for (PostId, GradeId)
            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_PostId_GradeId",
                table: "PostVersions",
                columns: new[] { "PostId", "GradeId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_PostVersions_PostId_GradeId", table: "PostVersions");

            migrationBuilder.AlterColumn<int>(
                name: "GradeId",
                table: "PostVersions",
                nullable: true,
                oldClrType: typeof(int));
        }
    }
}
