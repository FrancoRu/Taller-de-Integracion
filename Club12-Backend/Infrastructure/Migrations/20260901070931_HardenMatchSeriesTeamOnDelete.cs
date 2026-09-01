using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenMatchSeriesTeamOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchSeries_Teams_HomeTeamId",
                schema: "Club12",
                table: "MatchSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchSeries_Teams_VisitorTeamId",
                schema: "Club12",
                table: "MatchSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchSeries_Teams_WinningTeamId",
                schema: "Club12",
                table: "MatchSeries");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchSeries_Teams_HomeTeamId",
                schema: "Club12",
                table: "MatchSeries",
                column: "HomeTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchSeries_Teams_VisitorTeamId",
                schema: "Club12",
                table: "MatchSeries",
                column: "VisitorTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchSeries_Teams_WinningTeamId",
                schema: "Club12",
                table: "MatchSeries",
                column: "WinningTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchSeries_Teams_HomeTeamId",
                schema: "Club12",
                table: "MatchSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchSeries_Teams_VisitorTeamId",
                schema: "Club12",
                table: "MatchSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchSeries_Teams_WinningTeamId",
                schema: "Club12",
                table: "MatchSeries");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchSeries_Teams_HomeTeamId",
                schema: "Club12",
                table: "MatchSeries",
                column: "HomeTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchSeries_Teams_VisitorTeamId",
                schema: "Club12",
                table: "MatchSeries",
                column: "VisitorTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchSeries_Teams_WinningTeamId",
                schema: "Club12",
                table: "MatchSeries",
                column: "WinningTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id");
        }
    }
}
