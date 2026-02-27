using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddStageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Divisions_DivisionId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_PlayoffSeries_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Venues_VenueId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "PlayoffSeries",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_VenueId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GameNumber",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MatchWeek",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PlayoffSeriesId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "VenueId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "DivisionId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "StageId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Stages",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StageType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsElimination = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stages_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalSchema: "Club12",
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StageId",
                schema: "Club12",
                table: "Matches",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Stages_DivisionId",
                schema: "Club12",
                table: "Stages",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Divisions_DivisionId",
                schema: "Club12",
                table: "Matches",
                column: "DivisionId",
                principalSchema: "Club12",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Stages_StageId",
                schema: "Club12",
                table: "Matches",
                column: "StageId",
                principalSchema: "Club12",
                principalTable: "Stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Divisions_DivisionId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Stages_StageId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "Stages",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_Matches_StageId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "StageId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "DivisionId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GameNumber",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchWeek",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayoffSeriesId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VenueId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayoffSeries",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NextSeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    WinningTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GamesRequiredToWin = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamWins = table.Column<int>(type: "integer", nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    RoundName = table.Column<int>(type: "integer", nullable: false),
                    VisitorTeamWins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffSeries_PlayoffSeries_NextSeriesId",
                        column: x => x.NextSeriesId,
                        principalSchema: "Club12",
                        principalTable: "PlayoffSeries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayoffSeries_Teams_WinningTeamId",
                        column: x => x.WinningTeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches",
                column: "PlayoffSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_VenueId",
                schema: "Club12",
                table: "Matches",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeries_NextSeriesId",
                schema: "Club12",
                table: "PlayoffSeries",
                column: "NextSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeries_WinningTeamId",
                schema: "Club12",
                table: "PlayoffSeries",
                column: "WinningTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Divisions_DivisionId",
                schema: "Club12",
                table: "Matches",
                column: "DivisionId",
                principalSchema: "Club12",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_PlayoffSeries_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches",
                column: "PlayoffSeriesId",
                principalSchema: "Club12",
                principalTable: "PlayoffSeries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Venues_VenueId",
                schema: "Club12",
                table: "Matches",
                column: "VenueId",
                principalSchema: "Club12",
                principalTable: "Venues",
                principalColumn: "Id");
        }
    }
}
