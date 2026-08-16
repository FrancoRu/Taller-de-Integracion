using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPlayoffSeriesAndBracketName : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BestOf",
            schema: "Club12",
            table: "Stages",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "BracketName",
            schema: "Club12",
            table: "Stages",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RoundRobinLegs",
            schema: "Club12",
            table: "Stages",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "GameNumber",
            schema: "Club12",
            table: "Matches",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "SeriesId",
            schema: "Club12",
            table: "Matches",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsCrossDivisionCup",
            schema: "Club12",
            table: "Divisions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "MatchSeries",
            schema: "Club12",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StageId = table.Column<Guid>(type: "uuid", nullable: false),
                HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                VisitorTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                BestOf = table.Column<int>(type: "integer", nullable: false),
                WinningTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MatchSeries", x => x.Id);
                table.ForeignKey(
                    name: "FK_MatchSeries_Stages_StageId",
                    column: x => x.StageId,
                    principalSchema: "Club12",
                    principalTable: "Stages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MatchSeries_Teams_HomeTeamId",
                    column: x => x.HomeTeamId,
                    principalSchema: "Club12",
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MatchSeries_Teams_VisitorTeamId",
                    column: x => x.VisitorTeamId,
                    principalSchema: "Club12",
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MatchSeries_Teams_WinningTeamId",
                    column: x => x.WinningTeamId,
                    principalSchema: "Club12",
                    principalTable: "Teams",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Matches_SeriesId",
            schema: "Club12",
            table: "Matches",
            column: "SeriesId");

        migrationBuilder.CreateIndex(
            name: "IX_MatchSeries_CreatedAt",
            schema: "Club12",
            table: "MatchSeries",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_MatchSeries_HomeTeamId",
            schema: "Club12",
            table: "MatchSeries",
            column: "HomeTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_MatchSeries_StageId",
            schema: "Club12",
            table: "MatchSeries",
            column: "StageId");

        migrationBuilder.CreateIndex(
            name: "IX_MatchSeries_VisitorTeamId",
            schema: "Club12",
            table: "MatchSeries",
            column: "VisitorTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_MatchSeries_WinningTeamId",
            schema: "Club12",
            table: "MatchSeries",
            column: "WinningTeamId");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_MatchSeries_SeriesId",
            schema: "Club12",
            table: "Matches",
            column: "SeriesId",
            principalSchema: "Club12",
            principalTable: "MatchSeries",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Matches_MatchSeries_SeriesId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropTable(
            name: "MatchSeries",
            schema: "Club12");

        migrationBuilder.DropIndex(
            name: "IX_Matches_SeriesId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "BestOf",
            schema: "Club12",
            table: "Stages");

        migrationBuilder.DropColumn(
            name: "BracketName",
            schema: "Club12",
            table: "Stages");

        migrationBuilder.DropColumn(
            name: "RoundRobinLegs",
            schema: "Club12",
            table: "Stages");

        migrationBuilder.DropColumn(
            name: "GameNumber",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "SeriesId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "IsCrossDivisionCup",
            schema: "Club12",
            table: "Divisions");
    }
}
