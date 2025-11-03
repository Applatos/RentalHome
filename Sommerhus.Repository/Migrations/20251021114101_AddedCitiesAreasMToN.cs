using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddedCitiesAreasMToN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatures_Features_FeatureId1",
                table: "HouseFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_Houses_AreaId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatures_FeatureId1",
                table: "HouseFeatures");

            migrationBuilder.DropIndex(
                name: "IX_Areas_CityId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "FeatureId1",
                table: "HouseFeatures");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Areas");

            migrationBuilder.CreateTable(
                name: "AreaCities",
                columns: table => new
                {
                    AreaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CityId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaCities", x => new { x.AreaId, x.CityId });
                    table.ForeignKey(
                        name: "FK_AreaCities_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaCities_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseAreas",
                columns: table => new
                {
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AreaId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseAreas", x => new { x.HouseId, x.AreaId });
                    table.ForeignKey(
                        name: "FK_HouseAreas_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HouseAreas_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaCities_CityId",
                table: "AreaCities",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseAreas_AreaId",
                table: "HouseAreas",
                column: "AreaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaCities");

            migrationBuilder.DropTable(
                name: "HouseAreas");

            migrationBuilder.AddColumn<Guid>(
                name: "AreaId",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Houses",
                type: "TEXT",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FeatureId1",
                table: "HouseFeatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "Areas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Houses_AreaId",
                table: "Houses",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatures_FeatureId1",
                table: "HouseFeatures",
                column: "FeatureId1");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_CityId",
                table: "Areas",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatures_Features_FeatureId1",
                table: "HouseFeatures",
                column: "FeatureId1",
                principalTable: "Features",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }
    }
}
