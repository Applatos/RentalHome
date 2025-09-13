using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAndFeatureIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ZipCodes",
                table: "ZipCodes");

            migrationBuilder.RenameTable(
                name: "ZipCodes",
                newName: "ZipCode");

            migrationBuilder.RenameIndex(
                name: "IX_KindByHouse",
                table: "HouseImage",
                newName: "IX_HouseImage_HouseId_Kind");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "VacationHouse",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Feature",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Feature",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ZipCode",
                table: "ZipCode",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Feature_Key",
                table: "Feature",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZipCode_City",
                table: "ZipCode",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_ZipCode_Zip",
                table: "ZipCode",
                column: "Zip");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Feature_Key",
                table: "Feature");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ZipCode",
                table: "ZipCode");

            migrationBuilder.DropIndex(
                name: "IX_ZipCode_City",
                table: "ZipCode");

            migrationBuilder.DropIndex(
                name: "IX_ZipCode_Zip",
                table: "ZipCode");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "VacationHouse");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "Feature");

            migrationBuilder.RenameTable(
                name: "ZipCode",
                newName: "ZipCodes");

            migrationBuilder.RenameIndex(
                name: "IX_HouseImage_HouseId_Kind",
                table: "HouseImage",
                newName: "IX_KindByHouse");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Feature",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ZipCodes",
                table: "ZipCodes",
                column: "Id");
        }
    }
}
