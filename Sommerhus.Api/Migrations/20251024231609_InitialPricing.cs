using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RatePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                name: "RateModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: false),
                    Trigger = table.Column<int>(type: "INTEGER", nullable: false),
                    ThresholdNights = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateModifiers_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RateSeasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NightlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinStayNights = table.Column<int>(type: "INTEGER", nullable: true)
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
                name: "IX_RateModifiers_RatePlanId",
                table: "RateModifiers",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_HouseId",
                table: "RatePlans",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_RateSeasons_RatePlanId_StartDate_EndDate",
                table: "RateSeasons",
                columns: new[] { "RatePlanId", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateModifiers");

            migrationBuilder.DropTable(
                name: "RateSeasons");

            migrationBuilder.DropTable(
                name: "RatePlans");
        }
    }
}
