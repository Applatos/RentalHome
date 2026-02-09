using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sommerhus.Core.Migrations
{
    /// <inheritdoc />
    public partial class FlexibleCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeasonSpans_HouseGroups_GroupId",
                table: "SeasonSpans");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "SeasonSpans",
                newName: "CalendarId");

            migrationBuilder.RenameIndex(
                name: "IX_SeasonSpans_GroupId_StartDate_EndDate",
                table: "SeasonSpans",
                newName: "IX_SeasonSpans_CalendarId_StartDate_EndDate");

            migrationBuilder.AddColumn<Guid>(
                name: "CalendarOverrideId",
                table: "Houses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultCalendarId",
                table: "HouseGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SeasonCalendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonCalendars", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Houses_CalendarOverrideId",
                table: "Houses",
                column: "CalendarOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseGroups_DefaultCalendarId",
                table: "HouseGroups",
                column: "DefaultCalendarId");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseGroups_SeasonCalendars_DefaultCalendarId",
                table: "HouseGroups",
                column: "DefaultCalendarId",
                principalTable: "SeasonCalendars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_SeasonCalendars_CalendarOverrideId",
                table: "Houses",
                column: "CalendarOverrideId",
                principalTable: "SeasonCalendars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonSpans_SeasonCalendars_CalendarId",
                table: "SeasonSpans",
                column: "CalendarId",
                principalTable: "SeasonCalendars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseGroups_SeasonCalendars_DefaultCalendarId",
                table: "HouseGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_SeasonCalendars_CalendarOverrideId",
                table: "Houses");

            migrationBuilder.DropForeignKey(
                name: "FK_SeasonSpans_SeasonCalendars_CalendarId",
                table: "SeasonSpans");

            migrationBuilder.DropTable(
                name: "SeasonCalendars");

            migrationBuilder.DropIndex(
                name: "IX_Houses_CalendarOverrideId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_HouseGroups_DefaultCalendarId",
                table: "HouseGroups");

            migrationBuilder.DropColumn(
                name: "CalendarOverrideId",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "DefaultCalendarId",
                table: "HouseGroups");

            migrationBuilder.RenameColumn(
                name: "CalendarId",
                table: "SeasonSpans",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_SeasonSpans_CalendarId_StartDate_EndDate",
                table: "SeasonSpans",
                newName: "IX_SeasonSpans_GroupId_StartDate_EndDate");

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonSpans_HouseGroups_GroupId",
                table: "SeasonSpans",
                column: "GroupId",
                principalTable: "HouseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
