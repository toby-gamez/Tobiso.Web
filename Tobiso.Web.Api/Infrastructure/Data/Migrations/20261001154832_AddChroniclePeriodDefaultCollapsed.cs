using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChroniclePeriodDefaultCollapsed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultCollapsed",
                table: "ChroniclePeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCollapsed",
                table: "ChroniclePeriods");
        }
    }
}
