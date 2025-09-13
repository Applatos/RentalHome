using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feature",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ValueType = table.Column<int>(type: "INTEGER", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VacationHouse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Zip = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Facilities = table.Column<string>(type: "TEXT", nullable: true),
                    CoverImageId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationHouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZipCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Zip = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZipCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HouseFeatureValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeatureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValueBool = table.Column<bool>(type: "INTEGER", nullable: true),
                    ValueInt = table.Column<int>(type: "INTEGER", nullable: true),
                    ValueDecimal = table.Column<decimal>(type: "TEXT", nullable: true),
                    ValueText = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFeatureValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFeatureValues_Feature_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Feature",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HouseFeatureValues_VacationHouse_HouseId",
                        column: x => x.HouseId,
                        principalTable: "VacationHouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseImage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Alt = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseImage_VacationHouse_HouseId",
                        column: x => x.HouseId,
                        principalTable: "VacationHouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_FeatureId",
                table: "HouseFeatureValues",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues",
                columns: new[] { "HouseId", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KindByHouse",
                table: "HouseImage",
                columns: new[] { "HouseId", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseFeatureValues");

            migrationBuilder.DropTable(
                name: "HouseImage");

            migrationBuilder.DropTable(
                name: "ZipCodes");

            migrationBuilder.DropTable(
                name: "Feature");

            migrationBuilder.DropTable(
                name: "VacationHouse");
        }
    }
}
