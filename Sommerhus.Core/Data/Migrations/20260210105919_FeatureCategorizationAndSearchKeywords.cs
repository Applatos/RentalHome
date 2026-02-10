using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeatureCategorizationAndSearchKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchKeywords",
                table: "Houses",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSearchable",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "Features",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Features_IsSearchable",
                table: "Features",
                column: "IsSearchable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Features_IsSearchable",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "SearchKeywords",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "IsSearchable",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "Features");
        }
    }
}
