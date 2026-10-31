using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolClassesAndEventScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SchoolClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolClasses_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ClassGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassGroups_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassMemberships_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassMembershipGroups",
                columns: table => new
                {
                    MembershipId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMembershipGroups", x => new { x.MembershipId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_ClassMembershipGroups_ClassGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "ClassGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassMembershipGroups_ClassMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "ClassMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ClassId",
                table: "Events",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedByUserId",
                table: "Events",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupId",
                table: "Events",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Scope_StartDate",
                table: "Events",
                columns: new[] { "Scope", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_ClassId_Name",
                table: "ClassGroups",
                columns: new[] { "ClassId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassMembershipGroups_GroupId",
                table: "ClassMembershipGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMemberships_ClassId_Status",
                table: "ClassMemberships",
                columns: new[] { "ClassId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassMemberships_UserId",
                table: "ClassMemberships",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_GradeId",
                table: "SchoolClasses",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_Name",
                table: "SchoolClasses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_ClassGroups_GroupId",
                table: "Events",
                column: "GroupId",
                principalTable: "ClassGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_SchoolClasses_ClassId",
                table: "Events",
                column: "ClassId",
                principalTable: "SchoolClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Users_CreatedByUserId",
                table: "Events",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_ClassGroups_GroupId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_SchoolClasses_ClassId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Users_CreatedByUserId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "ClassMembershipGroups");

            migrationBuilder.DropTable(
                name: "ClassGroups");

            migrationBuilder.DropTable(
                name: "ClassMemberships");

            migrationBuilder.DropTable(
                name: "SchoolClasses");

            migrationBuilder.DropIndex(
                name: "IX_Events_ClassId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CreatedByUserId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_GroupId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Scope_StartDate",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Events");
        }
    }
}
