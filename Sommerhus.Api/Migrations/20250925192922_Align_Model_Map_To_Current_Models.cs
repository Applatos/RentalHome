using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Align_Model_Map_To_Current_Models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Cities_CityId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatureValues_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.DropIndex(
                name: "IX_AreaImages_AreaId",
                table: "AreaImages");

            migrationBuilder.AlterColumn<int>(
                name: "Kind",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues",
                columns: new[] { "HouseId", "FeatureId" },
                unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_Features_Key",
            //    table: "Features",
            //    column: "Key",
            //    unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaImages_AreaId_FileName",
                table: "AreaImages",
                columns: new[] { "AreaId", "FileName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Cities_CityId",
                table: "Houses",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Cities_CityId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_HouseFeatureValues_HouseId_FeatureId",
                table: "HouseFeatureValues");

            migrationBuilder.DropIndex(
                name: "IX_Features_Key",
                table: "Features");

            migrationBuilder.DropIndex(
                name: "IX_AreaImages_AreaId_FileName",
                table: "AreaImages");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Images",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFeatureValues_HouseId",
                table: "HouseFeatureValues",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaImages_AreaId",
                table: "AreaImages",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Areas_AreaId",
                table: "Houses",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Cities_CityId",
                table: "Houses",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
