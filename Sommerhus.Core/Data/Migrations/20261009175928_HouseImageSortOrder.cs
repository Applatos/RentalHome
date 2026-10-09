using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseImageSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Search documents now carry each feature's key, icon and English name. They are
            // derived data, so they are dropped here and rebuilt at startup.
            migrationBuilder.Sql("DELETE FROM HouseSearchDocuments;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Images");
        }
    }
}
