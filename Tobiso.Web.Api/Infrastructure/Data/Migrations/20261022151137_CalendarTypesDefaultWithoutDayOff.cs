using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CalendarTypesDefaultWithoutDayOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CalendarTypeMask",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 14,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 15);

            // Rows still on the old "everything" default (the feature is new) lose Volno too.
            migrationBuilder.Sql("UPDATE [Users] SET [CalendarTypeMask] = 14 WHERE [CalendarTypeMask] = 15");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CalendarTypeMask",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 15,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 14);
        }
    }
}
