using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class skibidi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_HouseId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_CityImages_CityId",
                table: "CityImages");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Images",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HouseId_FileName",
                table: "Images",
                columns: new[] { "HouseId", "FileName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_HouseId_FileName",
                table: "Images");

            migrationBuilder.AlterColumn<int>(
                name: "Kind",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HouseId",
                table: "Images",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CityImages_CityId",
                table: "CityImages",
                column: "CityId");
        }
    }
}
