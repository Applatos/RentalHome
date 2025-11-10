using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Zip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IconUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HouseGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeasonCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "AreaImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaImages_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaCities",
                columns: table => new
                {
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "CityImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Alt = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityImages_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Houses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Facilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Houses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Houses_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Houses_HouseGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "HouseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SeasonSpans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "HouseAreas",
                columns: table => new
                {
                    HouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "HouseFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFeatures_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Alt = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "PriceModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    ThresholdNights = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceModifiers_PricePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "PricePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PricePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricePlanId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NightlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_AreaCities_CityId",
                table: "AreaCities",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaImages_AreaId",
                table: "AreaImages",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CityImages_CityId",
                table: "CityImages",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Key",
                table: "Features",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseAreas_AreaId",
                table: "HouseAreas",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatures_FeatureId",
                table: "HouseFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatures_HouseId",
                table: "HouseFeatures",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Houses_CityId",
                table: "Houses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Houses_GroupId",
                table: "Houses",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HouseId",
                table: "Images",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceModifiers_RatePlanId",
                table: "PriceModifiers",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PricePlans_HouseId",
                table: "PricePlans",
                column: "HouseId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaCities");

            migrationBuilder.DropTable(
                name: "AreaImages");

            migrationBuilder.DropTable(
                name: "CityImages");

            migrationBuilder.DropTable(
                name: "HouseAreas");

            migrationBuilder.DropTable(
                name: "HouseFeatures");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "PriceModifiers");

            migrationBuilder.DropTable(
                name: "SeasonPrices");

            migrationBuilder.DropTable(
                name: "SeasonSpans");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "PricePlans");

            migrationBuilder.DropTable(
                name: "SeasonCodes");

            migrationBuilder.DropTable(
                name: "Houses");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "HouseGroups");
        }
    }
}
