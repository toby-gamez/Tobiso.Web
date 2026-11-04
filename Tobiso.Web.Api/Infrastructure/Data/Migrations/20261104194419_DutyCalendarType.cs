using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DutyCalendarType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CalendarTypeMask",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 14);

            // Duty is on by default for existing feeds too (bit 16).
            migrationBuilder.Sql("UPDATE [Users] SET [CalendarTypeMask] = [CalendarTypeMask] | 16");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CalendarTypeMask",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 14,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 30);
        }
    }
}
