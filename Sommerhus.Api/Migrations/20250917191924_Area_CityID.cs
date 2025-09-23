using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Area_CityID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "Areas",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Areas_CityId",
                table: "Areas",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Cities_CityId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_CityId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Areas");
        }
    }
}
