using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Migrations
{
    /// <inheritdoc />
    public partial class Phase20PriceSnapshotQuoteCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HousePriceSummaries",
                columns: table => new
                {
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MinNightlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MaxNightlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    ComputedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousePriceSummaries", x => x.HouseId);
                    table.ForeignKey(
                        name: "FK_HousePriceSummaries_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Guests = table.Column<int>(type: "INTEGER", nullable: false),
                    Nights = table.Column<int>(type: "INTEGER", nullable: false),
                    NightlyBreakdown = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Modifiers = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    PricePlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CalendarId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ComputedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceQuotes_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceQuotes_HouseId_CheckIn_CheckOut_Guests_ExpiresAtUtc",
                table: "PriceQuotes",
                columns: new[] { "HouseId", "CheckIn", "CheckOut", "Guests", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HousePriceSummaries");

            migrationBuilder.DropTable(
                name: "PriceQuotes");
        }
    }
}
