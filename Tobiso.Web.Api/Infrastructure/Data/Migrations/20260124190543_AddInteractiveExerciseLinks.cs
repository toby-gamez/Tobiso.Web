using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractiveExerciseLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InteractiveExercises_Posts_PostId",
                table: "InteractiveExercises");

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "InteractiveExercises",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "InteractiveExerciseCategories",
                columns: table => new
                {
                    InteractiveExerciseId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractiveExerciseCategories", x => new { x.InteractiveExerciseId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_InteractiveExerciseCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InteractiveExerciseCategories_InteractiveExercises_InteractiveExerciseId",
                        column: x => x.InteractiveExerciseId,
                        principalTable: "InteractiveExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InteractiveExercisePosts",
                columns: table => new
                {
                    InteractiveExerciseId = table.Column<int>(type: "int", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractiveExercisePosts", x => new { x.InteractiveExerciseId, x.PostId });
                    table.ForeignKey(
                        name: "FK_InteractiveExercisePosts_InteractiveExercises_InteractiveExerciseId",
                        column: x => x.InteractiveExerciseId,
                        principalTable: "InteractiveExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InteractiveExercisePosts_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveExerciseCategories_CategoryId",
                table: "InteractiveExerciseCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveExercisePosts_PostId",
                table: "InteractiveExercisePosts",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_InteractiveExercises_Posts_PostId",
                table: "InteractiveExercises",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InteractiveExercises_Posts_PostId",
                table: "InteractiveExercises");

            migrationBuilder.DropTable(
                name: "InteractiveExerciseCategories");

            migrationBuilder.DropTable(
                name: "InteractiveExercisePosts");

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "InteractiveExercises",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InteractiveExercises_Posts_PostId",
                table: "InteractiveExercises",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
