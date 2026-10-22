using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarToken",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalendarTypeMask",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CalendarToken",
                table: "Users",
                column: "CalendarToken",
                unique: true,
                filter: "[CalendarToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CalendarToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CalendarToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CalendarTypeMask",
                table: "Users");
        }
    }
}
