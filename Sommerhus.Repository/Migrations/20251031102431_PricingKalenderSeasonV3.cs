using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class PricingKalenderSeasonV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RateModifiers_PricePlans_RatePlanId",
                table: "RateModifiers");

            migrationBuilder.DropTable(
                name: "HouseSeasonSpans");

            migrationBuilder.DropTable(
                name: "SeasonRates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeasonCodes",
                table: "SeasonCodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RateModifiers",
                table: "RateModifiers");

            migrationBuilder.RenameTable(
                name: "RateModifiers",
                newName: "PriceModifiers");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SeasonCodes",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_RateModifiers_RatePlanId",
                table: "PriceModifiers",
                newName: "IX_PriceModifiers_RatePlanId");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeasonCodes",
                table: "SeasonCodes",
                column: "Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PriceModifiers",
                table: "PriceModifiers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SeasonPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PricePlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PricePlanId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    NightlyPrice = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonPrices_PricePlans_PricePlanId",
                        column: x => x.PricePlanId,
                        principalTable: "PricePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonPrices_PricePlans_PricePlanId1",
                        column: x => x.PricePlanId1,
                        principalTable: "PricePlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SeasonPrices_SeasonCodes_Code",
                        column: x => x.Code,
                        principalTable: "SeasonCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeasonSpans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonSpans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonSpans_HouseGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "HouseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonSpans_SeasonCodes_Code",
                        column: x => x.Code,
                        principalTable: "SeasonCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Houses_GroupId",
                table: "Houses",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonPrices_Code",
                table: "SeasonPrices",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonPrices_PricePlanId_Code",
                table: "SeasonPrices",
                columns: new[] { "PricePlanId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonPrices_PricePlanId1",
                table: "SeasonPrices",
                column: "PricePlanId1");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSpans_Code",
                table: "SeasonSpans",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSpans_GroupId_StartDate_EndDate",
                table: "SeasonSpans",
                columns: new[] { "GroupId", "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_HouseGroups_GroupId",
                table: "Houses",
                column: "GroupId",
                principalTable: "HouseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceModifiers_PricePlans_RatePlanId",
                table: "PriceModifiers",
                column: "RatePlanId",
                principalTable: "PricePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Houses_HouseGroups_GroupId",
                table: "Houses");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceModifiers_PricePlans_RatePlanId",
                table: "PriceModifiers");

            migrationBuilder.DropTable(
                name: "SeasonPrices");

            migrationBuilder.DropTable(
                name: "SeasonSpans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeasonCodes",
                table: "SeasonCodes");

            migrationBuilder.DropIndex(
                name: "IX_Houses_GroupId",
                table: "Houses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PriceModifiers",
                table: "PriceModifiers");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Houses");

            migrationBuilder.RenameTable(
                name: "PriceModifiers",
                newName: "RateModifiers");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "SeasonCodes",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_PriceModifiers_RatePlanId",
                table: "RateModifiers",
                newName: "IX_RateModifiers_RatePlanId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeasonCodes",
                table: "SeasonCodes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RateModifiers",
                table: "RateModifiers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "HouseSeasonSpans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseSeasonSpans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseSeasonSpans_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PricePlanId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NightlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    PricePlanId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonRates_PricePlans_PricePlanId",
                        column: x => x.PricePlanId,
                        principalTable: "PricePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonRates_PricePlans_PricePlanId1",
                        column: x => x.PricePlanId1,
                        principalTable: "PricePlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseSeasonSpans_HouseId_StartDate_EndDate",
                table: "HouseSeasonSpans",
                columns: new[] { "HouseId", "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonRates_PricePlanId_Code",
                table: "SeasonRates",
                columns: new[] { "PricePlanId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonRates_PricePlanId1",
                table: "SeasonRates",
                column: "PricePlanId1");

            migrationBuilder.AddForeignKey(
                name: "FK_RateModifiers_PricePlans_RatePlanId",
                table: "RateModifiers",
                column: "RatePlanId",
                principalTable: "PricePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
