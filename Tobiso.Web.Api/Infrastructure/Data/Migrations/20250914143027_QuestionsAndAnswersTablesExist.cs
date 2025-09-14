using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuestionsAndAnswersTablesExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tables already exist in database, no action needed
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed, tables already existed
        }
    }
}
