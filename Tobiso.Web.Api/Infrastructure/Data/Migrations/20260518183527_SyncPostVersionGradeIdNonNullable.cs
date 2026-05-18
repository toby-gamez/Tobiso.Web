using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPostVersionGradeIdNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostVersions_Grades_GradeId",
                table: "PostVersions");

            migrationBuilder.DropIndex(
                name: "IX_PostVersions_PostId",
                table: "PostVersions");

            migrationBuilder.AlterColumn<int>(
                name: "GradeId",
                table: "PostVersions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_PostId_GradeId",
                table: "PostVersions",
                columns: new[] { "PostId", "GradeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PostVersions_Grades_GradeId",
                table: "PostVersions",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostVersions_Grades_GradeId",
                table: "PostVersions");

            migrationBuilder.DropIndex(
                name: "IX_PostVersions_PostId_GradeId",
                table: "PostVersions");

            migrationBuilder.AlterColumn<int>(
                name: "GradeId",
                table: "PostVersions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_PostVersions_PostId",
                table: "PostVersions",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostVersions_Grades_GradeId",
                table: "PostVersions",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
