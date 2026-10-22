using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeEventColorOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Events",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldDefaultValue: "#007bff");

            // Null = follow the type's color. "#007bff" was the admin form's untouched default,
            // so those (and colors equal to their type's own) were never really picked.
            // Type: 0 DayOff, 1 Test, 2 Homework, 3 Other.
            migrationBuilder.Sql(@"
UPDATE [Events] SET [Color] = NULL
WHERE [Color] = '#007bff'
   OR ([Type] = 1 AND [Color] = '#d9534f')
   OR ([Type] = 2 AND [Color] = '#f0ad4e')
   OR ([Type] = 3 AND [Color] = '#5b8def');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Events] SET [Color] = CASE [Type]
    WHEN 1 THEN '#d9534f'
    WHEN 2 THEN '#f0ad4e'
    WHEN 3 THEN '#5b8def'
    ELSE '#007bff' END
WHERE [Color] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Events",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#007bff",
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldNullable: true);
        }
    }
}
