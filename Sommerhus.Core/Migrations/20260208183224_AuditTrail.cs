using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Migrations
{
    /// <inheritdoc />
    public partial class AuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeasonPrices_PricePlans_PricePlanId1",
                table: "SeasonPrices");

            migrationBuilder.DropIndex(
                name: "IX_SeasonPrices_PricePlanId1",
                table: "SeasonPrices");

            migrationBuilder.DropColumn(
                name: "PricePlanId1",
                table: "SeasonPrices");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Houses");

            migrationBuilder.RenameColumn(
                name: "UpdatedUtc",
                table: "PricePlans",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "PricePlans",
                newName: "CreatedAtUtc");

            migrationBuilder.DropColumn(
                name: "Facilities",
                table: "Houses");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Houses",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PricePlans",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PricePlans",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Houses",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Houses",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "HouseGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "HouseGroups",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "HouseGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "HouseGroups",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Features",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Features",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Features",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Features",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Cities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Cities",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Cities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Cities",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Areas",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Areas",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Areas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Areas",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Changes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ChangedAtUtc",
                table: "AuditEntries",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityType_EntityId",
                table: "AuditEntries",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PricePlans");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PricePlans");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "HouseGroups");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "HouseGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "HouseGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "HouseGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Areas");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "PricePlans",
                newName: "UpdatedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "PricePlans",
                newName: "CreatedUtc");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Houses");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Houses",
                newName: "CreatedUtc");

            migrationBuilder.AddColumn<string>(
                name: "Facilities",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PricePlanId1",
                table: "SeasonPrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CoverImageId",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonPrices_PricePlanId1",
                table: "SeasonPrices",
                column: "PricePlanId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonPrices_PricePlans_PricePlanId1",
                table: "SeasonPrices",
                column: "PricePlanId1",
                principalTable: "PricePlans",
                principalColumn: "Id");
        }
    }
}
