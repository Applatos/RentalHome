using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseSearchDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseSearchDocuments",
                columns: table => new
                {
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CityZip = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AreaNames = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SearchKeywords = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FeatureJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MinNightlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MaxNightlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    Bedrooms = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxGuests = table.Column<int>(type: "INTEGER", nullable: true),
                    HasPool = table.Column<bool>(type: "INTEGER", nullable: false),
                    PetFriendly = table.Column<bool>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    SearchVector = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseSearchDocuments", x => x.HouseId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseSearchDocuments_CityName",
                table: "HouseSearchDocuments",
                column: "CityName");

            migrationBuilder.CreateIndex(
                name: "IX_HouseSearchDocuments_MaxNightlyPrice",
                table: "HouseSearchDocuments",
                column: "MaxNightlyPrice");

            migrationBuilder.CreateIndex(
                name: "IX_HouseSearchDocuments_MinNightlyPrice",
                table: "HouseSearchDocuments",
                column: "MinNightlyPrice");

            migrationBuilder.CreateIndex(
                name: "IX_HouseSearchDocuments_Status",
                table: "HouseSearchDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HouseSearchDocuments_UpdatedAtUtc",
                table: "HouseSearchDocuments",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseSearchDocuments");
        }
    }
}
