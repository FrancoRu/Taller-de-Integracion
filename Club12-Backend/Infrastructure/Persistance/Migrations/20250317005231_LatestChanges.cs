using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class LatestChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RoundName",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "Seed",
                schema: "Club12",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "HomeTeamId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PlayoffSeriesId",
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
                    RoundName = table.Column<int>(type: "integer", nullable: false),
                    WinningTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    GamesRequiredToWin = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamWins = table.Column<int>(type: "integer", nullable: false),
                    VisitorTeamWins = table.Column<int>(type: "integer", nullable: false),
                    NextSeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "FK_Matches_PlayoffSeries_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches",
                column: "PlayoffSeriesId",
                principalSchema: "Club12",
                principalTable: "PlayoffSeries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                schema: "Club12",
                table: "Matches",
                column: "HomeTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_PlayoffSeries_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "PlayoffSeries",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PlayoffSeriesId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Seed",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "PlayoffSeriesId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "HomeTeamId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoundName",
                schema: "Club12",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                schema: "Club12",
                table: "Matches",
                column: "HomeTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
