using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Club12");

            migrationBuilder.CreateTable(
                name: "Divisions",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Height = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sanctions",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sanctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statistics",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ThreeLetterCode = table.Column<string>(type: "text", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalSchema: "Club12",
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanctionPlayers",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SanctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanctionPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SanctionPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "Club12",
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanctionPlayers_Sanctions_SanctionId",
                        column: x => x.SanctionId,
                        principalSchema: "Club12",
                        principalTable: "Sanctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    VisitorScore = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    WinningTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_VisitorTeamId",
                        column: x => x.VisitorTeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_WinningTeamId",
                        column: x => x.WinningTeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTeams",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JerseyNumber = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTeams_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "Club12",
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StandingsSummaries",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingsSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandingsSummaries_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandingsSummaries_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "Club12",
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayersStatistics",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatisticId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayersStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayersStatistics_Matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "Club12",
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayersStatistics_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "Club12",
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayersStatistics_Statistics_StatisticId",
                        column: x => x.StatisticId,
                        principalSchema: "Club12",
                        principalTable: "Statistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId",
                schema: "Club12",
                table: "Matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_VisitorTeamId",
                schema: "Club12",
                table: "Matches",
                column: "VisitorTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinningTeamId",
                schema: "Club12",
                table: "Matches",
                column: "WinningTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayersStatistics_MatchId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayersStatistics_PlayerId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayersStatistics_StatisticId",
                schema: "Club12",
                table: "PlayersStatistics",
                column: "StatisticId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeams_PlayerId",
                schema: "Club12",
                table: "PlayerTeams",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeams_TeamId",
                schema: "Club12",
                table: "PlayerTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SanctionPlayers_PlayerId",
                schema: "Club12",
                table: "SanctionPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SanctionPlayers_SanctionId",
                schema: "Club12",
                table: "SanctionPlayers",
                column: "SanctionId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingsSummaries_TeamId",
                schema: "Club12",
                table: "StandingsSummaries",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingsSummaries_TournamentId",
                schema: "Club12",
                table: "StandingsSummaries",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId",
                schema: "Club12",
                table: "Teams",
                column: "DivisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayersStatistics",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "PlayerTeams",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "SanctionPlayers",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "StandingsSummaries",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Matches",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Statistics",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Players",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Sanctions",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Tournaments",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "Club12");

            migrationBuilder.DropTable(
                name: "Divisions",
                schema: "Club12");
        }
    }
}
