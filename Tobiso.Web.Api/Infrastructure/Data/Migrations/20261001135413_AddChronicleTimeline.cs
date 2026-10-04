using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tobiso.Web.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChronicleTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChronicleAxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleAxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleCategories_ChronicleCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ChronicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartYear = table.Column<long>(type: "bigint", nullable: false),
                    EndYear = table.Column<long>(type: "bigint", nullable: true),
                    Precision = table.Column<int>(type: "int", nullable: false),
                    Reliability = table.Column<int>(type: "int", nullable: false),
                    ReliabilityNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Importance = table.Column<byte>(type: "tinyint", nullable: false),
                    MinGradeId = table.Column<int>(type: "int", nullable: true),
                    LessonSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleItems_Grades_MinGradeId",
                        column: x => x.MinGradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChroniclePeriodizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChroniclePeriodizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChroniclePolities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartYear = table.Column<long>(type: "bigint", nullable: false),
                    EndYear = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChroniclePolities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleRegions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleRegions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleRegions_ChronicleRegions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ChronicleRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleAxisEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AxisId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    ExpandChildren = table.Column<bool>(type: "bit", nullable: false),
                    OrderOverride = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleAxisEntries", x => x.Id);
                    table.CheckConstraint("CK_ChronicleAxisEntry_ItemOrCategory", "([ItemId] IS NOT NULL AND [CategoryId] IS NULL) OR ([ItemId] IS NULL AND [CategoryId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ChronicleAxisEntries_ChronicleAxes_AxisId",
                        column: x => x.AxisId,
                        principalTable: "ChronicleAxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChronicleAxisEntries_ChronicleCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ChronicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChronicleAxisEntries_ChronicleItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleEventPersons",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleEventPersons", x => new { x.EventId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_ChronicleEventPersons_ChronicleItems_EventId",
                        column: x => x.EventId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChronicleEventPersons_ChronicleItems_PersonId",
                        column: x => x.PersonId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChronicleItemCategories",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleItemCategories", x => new { x.ItemId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_ChronicleItemCategories_ChronicleCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ChronicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChronicleItemCategories_ChronicleItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleItemLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromId = table.Column<int>(type: "int", nullable: false),
                    ToId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleItemLinks", x => x.Id);
                    table.CheckConstraint("CK_ChronicleItemLink_DifferentItems", "[FromId] <> [ToId]");
                    table.ForeignKey(
                        name: "FK_ChronicleItemLinks_ChronicleItems_FromId",
                        column: x => x.FromId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChronicleItemLinks_ChronicleItems_ToId",
                        column: x => x.ToId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChronicleSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleSources_ChronicleItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChroniclePeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodizationId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartYear = table.Column<long>(type: "bigint", nullable: false),
                    StartEventId = table.Column<int>(type: "int", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChroniclePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChroniclePeriods_ChronicleItems_StartEventId",
                        column: x => x.StartEventId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChroniclePeriods_ChroniclePeriodizations_PeriodizationId",
                        column: x => x.PeriodizationId,
                        principalTable: "ChroniclePeriodizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleItemRegions",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleItemRegions", x => new { x.ItemId, x.RegionId });
                    table.ForeignKey(
                        name: "FK_ChronicleItemRegions_ChronicleItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ChronicleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChronicleItemRegions_ChronicleRegions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "ChronicleRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChroniclePolityTerritories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolityId = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: false),
                    Coverage = table.Column<int>(type: "int", nullable: false),
                    FromYear = table.Column<long>(type: "bigint", nullable: true),
                    ToYear = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChroniclePolityTerritories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChroniclePolityTerritories_ChroniclePolities_PolityId",
                        column: x => x.PolityId,
                        principalTable: "ChroniclePolities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChroniclePolityTerritories_ChronicleRegions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "ChronicleRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleAxes_Slug",
                table: "ChronicleAxes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleAxisEntries_AxisId",
                table: "ChronicleAxisEntries",
                column: "AxisId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleAxisEntries_CategoryId",
                table: "ChronicleAxisEntries",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleAxisEntries_ItemId",
                table: "ChronicleAxisEntries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleCategories_ParentId",
                table: "ChronicleCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleEventPersons_PersonId",
                table: "ChronicleEventPersons",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItemCategories_CategoryId",
                table: "ChronicleItemCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItemLinks_FromId_ToId_Type",
                table: "ChronicleItemLinks",
                columns: new[] { "FromId", "ToId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItemLinks_ToId",
                table: "ChronicleItemLinks",
                column: "ToId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItemRegions_RegionId",
                table: "ChronicleItemRegions",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItems_MinGradeId",
                table: "ChronicleItems",
                column: "MinGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItems_Slug",
                table: "ChronicleItems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleItems_StartYear_EndYear",
                table: "ChronicleItems",
                columns: new[] { "StartYear", "EndYear" });

            migrationBuilder.CreateIndex(
                name: "IX_ChroniclePeriods_PeriodizationId_Order",
                table: "ChroniclePeriods",
                columns: new[] { "PeriodizationId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChroniclePeriods_StartEventId",
                table: "ChroniclePeriods",
                column: "StartEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ChroniclePolityTerritories_PolityId_RegionId",
                table: "ChroniclePolityTerritories",
                columns: new[] { "PolityId", "RegionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChroniclePolityTerritories_RegionId",
                table: "ChroniclePolityTerritories",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleRegions_ParentId",
                table: "ChronicleRegions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleRegions_Path",
                table: "ChronicleRegions",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleSources_ItemId",
                table: "ChronicleSources",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChronicleAxisEntries");

            migrationBuilder.DropTable(
                name: "ChronicleEventPersons");

            migrationBuilder.DropTable(
                name: "ChronicleItemCategories");

            migrationBuilder.DropTable(
                name: "ChronicleItemLinks");

            migrationBuilder.DropTable(
                name: "ChronicleItemRegions");

            migrationBuilder.DropTable(
                name: "ChroniclePeriods");

            migrationBuilder.DropTable(
                name: "ChroniclePolityTerritories");

            migrationBuilder.DropTable(
                name: "ChronicleSources");

            migrationBuilder.DropTable(
                name: "ChronicleAxes");

            migrationBuilder.DropTable(
                name: "ChronicleCategories");

            migrationBuilder.DropTable(
                name: "ChroniclePeriodizations");

            migrationBuilder.DropTable(
                name: "ChroniclePolities");

            migrationBuilder.DropTable(
                name: "ChronicleRegions");

            migrationBuilder.DropTable(
                name: "ChronicleItems");
        }
    }
}
