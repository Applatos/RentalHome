using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteToAreaImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.AddForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues");

            migrationBuilder.AddForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseFeatureValues_Houses_HouseId",
                table: "HouseFeatureValues",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "Id");
        }
    }
}
