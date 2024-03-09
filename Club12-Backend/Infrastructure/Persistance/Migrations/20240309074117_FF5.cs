using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class FF5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordHashed",
                schema: "Club12",
                table: "Users",
                newName: "Password");

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Tournaments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Tournaments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Statistics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Statistics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "StandingsSummaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "StandingsSummaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Sanctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Sanctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "SanctionPlayers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "SanctionPlayers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "PlayersStatistics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "PlayersStatistics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "Club12",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Divisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Divisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_UserCreatedId",
                schema: "Club12",
                table: "Tournaments",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_UserUpdatedId",
                schema: "Club12",
                table: "Tournaments",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_UserCreatedId",
                schema: "Club12",
                table: "Teams",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_UserUpdatedId",
                schema: "Club12",
                table: "Teams",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_UserCreatedId",
                schema: "Club12",
                table: "Statistics",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_UserUpdatedId",
                schema: "Club12",
                table: "Statistics",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingsSummaries_UserCreatedId",
                schema: "Club12",
                table: "StandingsSummaries",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingsSummaries_UserUpdatedId",
                schema: "Club12",
                table: "StandingsSummaries",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Sanctions_UserCreatedId",
                schema: "Club12",
                table: "Sanctions",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Sanctions_UserUpdatedId",
                schema: "Club12",
                table: "Sanctions",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_SanctionPlayers_UserCreatedId",
                schema: "Club12",
                table: "SanctionPlayers",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_SanctionPlayers_UserUpdatedId",
                schema: "Club12",
                table: "SanctionPlayers",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayersStatistics_UserCreatedId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayersStatistics_UserUpdatedId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserCreatedId",
                schema: "Club12",
                table: "Players",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserUpdatedId",
                schema: "Club12",
                table: "Players",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserCreatedId",
                schema: "Club12",
                table: "Matches",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserUpdatedId",
                schema: "Club12",
                table: "Matches",
                column: "UserUpdatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_UserCreatedId",
                schema: "Club12",
                table: "Divisions",
                column: "UserCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_UserUpdatedId",
                schema: "Club12",
                table: "Divisions",
                column: "UserUpdatedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Divisions_Users_UserCreatedId",
                schema: "Club12",
                table: "Divisions",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Divisions_Users_UserUpdatedId",
                schema: "Club12",
                table: "Divisions",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserCreatedId",
                schema: "Club12",
                table: "Matches",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserUpdatedId",
                schema: "Club12",
                table: "Matches",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Users_UserCreatedId",
                schema: "Club12",
                table: "Players",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Users_UserUpdatedId",
                schema: "Club12",
                table: "Players",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayersStatistics_Users_UserCreatedId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayersStatistics_Users_UserUpdatedId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SanctionPlayers_Users_UserCreatedId",
                schema: "Club12",
                table: "SanctionPlayers",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SanctionPlayers_Users_UserUpdatedId",
                schema: "Club12",
                table: "SanctionPlayers",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sanctions_Users_UserCreatedId",
                schema: "Club12",
                table: "Sanctions",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sanctions_Users_UserUpdatedId",
                schema: "Club12",
                table: "Sanctions",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StandingsSummaries_Users_UserCreatedId",
                schema: "Club12",
                table: "StandingsSummaries",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StandingsSummaries_Users_UserUpdatedId",
                schema: "Club12",
                table: "StandingsSummaries",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Statistics_Users_UserCreatedId",
                schema: "Club12",
                table: "Statistics",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Statistics_Users_UserUpdatedId",
                schema: "Club12",
                table: "Statistics",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_UserCreatedId",
                schema: "Club12",
                table: "Teams",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_UserUpdatedId",
                schema: "Club12",
                table: "Teams",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Users_UserCreatedId",
                schema: "Club12",
                table: "Tournaments",
                column: "UserCreatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Users_UserUpdatedId",
                schema: "Club12",
                table: "Tournaments",
                column: "UserUpdatedId",
                principalSchema: "Club12",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Divisions_Users_UserCreatedId",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Divisions_Users_UserUpdatedId",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserCreatedId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserUpdatedId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Users_UserCreatedId",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Users_UserUpdatedId",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayersStatistics_Users_UserCreatedId",
                schema: "Club12",
                table: "PlayersStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayersStatistics_Users_UserUpdatedId",
                schema: "Club12",
                table: "PlayersStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_SanctionPlayers_Users_UserCreatedId",
                schema: "Club12",
                table: "SanctionPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_SanctionPlayers_Users_UserUpdatedId",
                schema: "Club12",
                table: "SanctionPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_Sanctions_Users_UserCreatedId",
                schema: "Club12",
                table: "Sanctions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sanctions_Users_UserUpdatedId",
                schema: "Club12",
                table: "Sanctions");

            migrationBuilder.DropForeignKey(
                name: "FK_StandingsSummaries_Users_UserCreatedId",
                schema: "Club12",
                table: "StandingsSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_StandingsSummaries_Users_UserUpdatedId",
                schema: "Club12",
                table: "StandingsSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Statistics_Users_UserCreatedId",
                schema: "Club12",
                table: "Statistics");

            migrationBuilder.DropForeignKey(
                name: "FK_Statistics_Users_UserUpdatedId",
                schema: "Club12",
                table: "Statistics");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_UserCreatedId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_UserUpdatedId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Users_UserCreatedId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Users_UserUpdatedId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_UserCreatedId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_UserUpdatedId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Teams_UserCreatedId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_UserUpdatedId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Statistics_UserCreatedId",
                schema: "Club12",
                table: "Statistics");

            migrationBuilder.DropIndex(
                name: "IX_Statistics_UserUpdatedId",
                schema: "Club12",
                table: "Statistics");

            migrationBuilder.DropIndex(
                name: "IX_StandingsSummaries_UserCreatedId",
                schema: "Club12",
                table: "StandingsSummaries");

            migrationBuilder.DropIndex(
                name: "IX_StandingsSummaries_UserUpdatedId",
                schema: "Club12",
                table: "StandingsSummaries");

            migrationBuilder.DropIndex(
                name: "IX_Sanctions_UserCreatedId",
                schema: "Club12",
                table: "Sanctions");

            migrationBuilder.DropIndex(
                name: "IX_Sanctions_UserUpdatedId",
                schema: "Club12",
                table: "Sanctions");

            migrationBuilder.DropIndex(
                name: "IX_SanctionPlayers_UserCreatedId",
                schema: "Club12",
                table: "SanctionPlayers");

            migrationBuilder.DropIndex(
                name: "IX_SanctionPlayers_UserUpdatedId",
                schema: "Club12",
                table: "SanctionPlayers");

            migrationBuilder.DropIndex(
                name: "IX_PlayersStatistics_UserCreatedId",
                schema: "Club12",
                table: "PlayersStatistics");

            migrationBuilder.DropIndex(
                name: "IX_PlayersStatistics_UserUpdatedId",
                schema: "Club12",
                table: "PlayersStatistics");

            migrationBuilder.DropIndex(
                name: "IX_Players_UserCreatedId",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_UserUpdatedId",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Matches_UserCreatedId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_UserUpdatedId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_UserCreatedId",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_UserUpdatedId",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Statistics");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Statistics");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "StandingsSummaries");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "StandingsSummaries");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Sanctions");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Sanctions");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "SanctionPlayers");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "SanctionPlayers");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "PlayersStatistics");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "PlayersStatistics");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "UserCreatedId",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "UserUpdatedId",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.RenameColumn(
                name: "Password",
                schema: "Club12",
                table: "Users",
                newName: "PasswordHashed");
        }
    }
}
