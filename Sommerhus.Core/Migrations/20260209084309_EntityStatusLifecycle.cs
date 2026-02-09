using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Migrations
{
    /// <inheritdoc />
    public partial class EntityStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Houses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Areas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Houses_Status",
                table: "Houses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Status",
                table: "Areas",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Houses_Status",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_Areas_Status",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Areas");
        }
    }
}
