using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class PricingKalenderSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RateModifiers_RatePlans_RatePlanId",
                table: "RateModifiers");

            migrationBuilder.DropTable(
                name: "RateSeasons");

            migrationBuilder.DropTable(
                name: "RatePlans");

            migrationBuilder.CreateTable(
                name: "HouseSeasonSpans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "PricePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricePlans_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeasonRates",
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
                name: "IX_PricePlans_HouseId",
                table: "PricePlans",
                column: "HouseId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RateModifiers_PricePlans_RatePlanId",
                table: "RateModifiers");

            migrationBuilder.DropTable(
                name: "HouseSeasonSpans");

            migrationBuilder.DropTable(
                name: "SeasonCodes");

            migrationBuilder.DropTable(
                name: "SeasonRates");

            migrationBuilder.DropTable(
                name: "PricePlans");

            migrationBuilder.CreateTable(
                name: "RatePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatePlans_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RateSeasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MinStayNights = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NightlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateSeasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateSeasons_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_HouseId",
                table: "RatePlans",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_RateSeasons_RatePlanId_StartDate_EndDate",
                table: "RateSeasons",
                columns: new[] { "RatePlanId", "StartDate", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_RateModifiers_RatePlans_RatePlanId",
                table: "RateModifiers",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
