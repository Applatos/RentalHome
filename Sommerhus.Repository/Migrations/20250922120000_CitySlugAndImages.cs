using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CitySlugAndImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Cities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Cities SET Slug = lower(hex(Id));");

            migrationBuilder.CreateTable(
                name: "CityImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Alt = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Slug",
                table: "Cities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CityImages_CityId",
                table: "CityImages",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CityImages_CityId_FileName",
                table: "CityImages",
                columns: new[] { "CityId", "FileName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityImages");

            migrationBuilder.DropIndex(
                name: "IX_Cities_Slug",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Cities");
        }
    }
}
