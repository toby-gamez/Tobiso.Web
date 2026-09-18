using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPostVersionIsChecked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The "Zkontrolováno" badge used to be driven purely by LastFix.HasValue.
            // Now that IsChecked is the single source of truth, carry that historical
            // signal forward one time so existing reviewed versions don't lose the badge.
            migrationBuilder.Sql(
                "UPDATE PostVersions SET IsChecked = 1 WHERE LastFix IS NOT NULL AND IsChecked IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE PostVersions SET IsChecked = NULL WHERE LastFix IS NOT NULL AND IsChecked = 1");
        }
    }
}
