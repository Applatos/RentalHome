using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class simplified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Features_FeatureId",
                table: "HouseFeatureValues");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_Images_HouseId_FileName",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_CityImages_CityId_FileName",
                table: "CityImages");

            migrationBuilder.DropIndex(
                name: "IX_AreaImages_AreaId_FileName",
                table: "AreaImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HouseFeatureValues",
                table: "HouseFeatureValues");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues");

            migrationBuilder.RenameTable(
                name: "HouseFeatureValues",
                newName: "HouseFeatures");

            migrationBuilder.RenameIndex(
                name: "IX_HouseFeatureValues_FeatureId",
                table: "HouseFeatures",
                newName: "IX_HouseFeatures_FeatureId");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Features",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 60);

            migrationBuilder.AddColumn<Guid>(
                name: "FeatureId1",
                table: "HouseFeatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HouseFeatures",
                table: "HouseFeatures",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HouseId",
                table: "Images",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CityImages_CityId",
                table: "CityImages",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaImages_AreaId",
                table: "AreaImages",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatures_FeatureId1",
                table: "HouseFeatures",
                column: "FeatureId1");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatures_HouseId",
                table: "HouseFeatures",
                column: "HouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatures_Features_FeatureId",
                table: "HouseFeatures",
                column: "FeatureId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatures_Features_FeatureId1",
                table: "HouseFeatures",
                column: "FeatureId1",
                principalTable: "Features",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatures_Houses_HouseId",
                table: "HouseFeatures",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatures_Features_FeatureId",
                table: "HouseFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatures_Features_FeatureId1",
                table: "HouseFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatures_Houses_HouseId",
                table: "HouseFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_Images_HouseId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_CityImages_CityId",
                table: "CityImages");

            migrationBuilder.DropIndex(
                name: "IX_AreaImages_AreaId",
                table: "AreaImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HouseFeatures",
                table: "HouseFeatures");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatures_FeatureId1",
                table: "HouseFeatures");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatures_HouseId",
                table: "HouseFeatures");

            migrationBuilder.DropColumn(
                name: "FeatureId1",
                table: "HouseFeatures");

            migrationBuilder.RenameTable(
                name: "HouseFeatures",
                newName: "HouseFeatureValues");

            migrationBuilder.RenameIndex(
                name: "IX_HouseFeatures_FeatureId",
                table: "HouseFeatureValues",
                newName: "IX_HouseFeatureValues_FeatureId");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Features",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 60,
                oldCollation: "NOCASE");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HouseFeatureValues",
                table: "HouseFeatureValues",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HouseId_FileName",
                table: "Images",
                columns: new[] { "HouseId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CityImages_CityId_FileName",
                table: "CityImages",
                columns: new[] { "CityId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaImages_AreaId_FileName",
                table: "AreaImages",
                columns: new[] { "AreaId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues",
                columns: new[] { "HouseId", "FeatureId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

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
                name: "FK_Houses_Areas_AreaId",
                table: "Houses",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
