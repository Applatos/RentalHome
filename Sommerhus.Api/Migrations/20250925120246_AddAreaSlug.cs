using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Areas",
                type: "TEXT",
                maxLength: 140,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE Areas
SET Slug = (
    CASE
        WHEN Name IS NULL OR trim(Name) = '' THEN 'area-' || substr(Id, 1, 8)
        ELSE (
                replace(
                    replace(
                        replace(
                            replace(
                                replace(lower(trim(Name)), 'æ', 'ae'),
                                'ø', 'o'),
                            'å', 'a'),
                        ' ', '-'),
                    '''', '') || '-' || substr(Id, 1, 4)
        )
    END
);

UPDATE Areas
SET Slug = REPLACE(Slug, ""--"", ""-"")
WHERE Slug LIKE '%--%';

UPDATE Areas AS a
SET Slug = a.Slug || '-' || substr(a.Id, 5, 4)
WHERE EXISTS (
    SELECT 1 FROM Areas b
    WHERE b.Id <> a.Id AND b.Slug = a.Slug
);
");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Slug",
                table: "Areas",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Areas_Slug",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Areas");
        }
    }
}
