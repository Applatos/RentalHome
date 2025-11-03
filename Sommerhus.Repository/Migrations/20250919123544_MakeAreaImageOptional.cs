using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class MakeAreaImageOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages");

            migrationBuilder.AlterColumn<Guid>(
                name: "AreaId",
                table: "AreaImages",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages");

            migrationBuilder.AlterColumn<Guid>(
                name: "AreaId",
                table: "AreaImages",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AreaImages_Areas_AreaId",
                table: "AreaImages",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
