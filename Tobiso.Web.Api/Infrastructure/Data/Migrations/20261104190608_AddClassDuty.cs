using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassDuty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DutyPersonId",
                table: "ClassMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DutyPeople",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DutyPeople", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DutyPeople_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DutyPairs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PersonAId = table.Column<int>(type: "int", nullable: false),
                    PersonBId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DutyPairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DutyPairs_DutyPeople_PersonAId",
                        column: x => x.PersonAId,
                        principalTable: "DutyPeople",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DutyPairs_DutyPeople_PersonBId",
                        column: x => x.PersonBId,
                        principalTable: "DutyPeople",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DutyPairs_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DutyWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PairId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DutyWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DutyWeeks_DutyPairs_PairId",
                        column: x => x.PairId,
                        principalTable: "DutyPairs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DutyWeeks_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassMemberships_DutyPersonId",
                table: "ClassMemberships",
                column: "DutyPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPairs_ClassId_Position",
                table: "DutyPairs",
                columns: new[] { "ClassId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_DutyPairs_PersonAId",
                table: "DutyPairs",
                column: "PersonAId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPairs_PersonBId",
                table: "DutyPairs",
                column: "PersonBId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeople_ClassId_Name",
                table: "DutyPeople",
                columns: new[] { "ClassId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DutyWeeks_ClassId_WeekStart",
                table: "DutyWeeks",
                columns: new[] { "ClassId", "WeekStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DutyWeeks_PairId",
                table: "DutyWeeks",
                column: "PairId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassMemberships_DutyPeople_DutyPersonId",
                table: "ClassMemberships",
                column: "DutyPersonId",
                principalTable: "DutyPeople",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassMemberships_DutyPeople_DutyPersonId",
                table: "ClassMemberships");

            migrationBuilder.DropTable(
                name: "DutyWeeks");

            migrationBuilder.DropTable(
                name: "DutyPairs");

            migrationBuilder.DropTable(
                name: "DutyPeople");

            migrationBuilder.DropIndex(
                name: "IX_ClassMemberships_DutyPersonId",
                table: "ClassMemberships");

            migrationBuilder.DropColumn(
                name: "DutyPersonId",
                table: "ClassMemberships");
        }
    }
}
