using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCityImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Feature_FeatureId",
                table: "HouseFeatureValues");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_VacationHouse_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseImage_VacationHouse_HouseId",
                table: "HouseImage");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VacationHouse",
                table: "VacationHouse");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HouseImage",
                table: "HouseImage");

            migrationBuilder.DropIndex(
                name: "IX_HouseImage_HouseId_Kind",
                table: "HouseImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Feature",
                table: "Feature");

            migrationBuilder.DropIndex(
                name: "IX_Feature_Key",
                table: "Feature");

            migrationBuilder.RenameTable(
                name: "VacationHouse",
                newName: "Houses");

            migrationBuilder.RenameTable(
                name: "HouseImage",
                newName: "Images");

            migrationBuilder.RenameTable(
                name: "Feature",
                newName: "Features");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                table: "Houses",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Houses",
                table: "Houses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Images",
                table: "Images",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Features",
                table: "Features",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Zip = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CityImage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityImage_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_HouseId",
                table: "HouseFeatureValues",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HouseId",
                table: "Images",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_City_Name",
                table: "City",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_City_Slug",
                table: "City",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_City_Zip",
                table: "City",
                column: "Zip");

            migrationBuilder.CreateIndex(
                name: "IX_CityImage_CityId_SortOrder",
                table: "CityImage",
                columns: new[] { "CityId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatureValues_Features_FeatureId",
                table: "HouseFeatureValues",
                column: "FeatureId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Houses_HouseId",
                table: "Images",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Features_FeatureId",
                table: "HouseFeatureValues");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.DropForeignKey(
                name: "FK_Images_Houses_HouseId",
                table: "Images");

            migrationBuilder.DropTable(
                name: "CityImage");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatureValues_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Images",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_HouseId",
                table: "Images");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Houses",
                table: "Houses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Features",
                table: "Features");

            migrationBuilder.RenameTable(
                name: "Images",
                newName: "HouseImage");

            migrationBuilder.RenameTable(
                name: "Houses",
                newName: "VacationHouse");

            migrationBuilder.RenameTable(
                name: "Features",
                newName: "Feature");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                table: "VacationHouse",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Feature",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HouseImage",
                table: "HouseImage",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VacationHouse",
                table: "VacationHouse",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Feature",
                table: "Feature",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues",
                columns: new[] { "HouseId", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseImage_HouseId_Kind",
                table: "HouseImage",
                columns: new[] { "HouseId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_Feature_Key",
                table: "Feature",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatureValues_Feature_FeatureId",
                table: "HouseFeatureValues",
                column: "FeatureId",
                principalTable: "Feature",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatureValues_VacationHouse_HouseId",
                table: "HouseFeatureValues",
                column: "HouseId",
                principalTable: "VacationHouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseImage_VacationHouse_HouseId",
                table: "HouseImage",
                column: "HouseId",
                principalTable: "VacationHouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
