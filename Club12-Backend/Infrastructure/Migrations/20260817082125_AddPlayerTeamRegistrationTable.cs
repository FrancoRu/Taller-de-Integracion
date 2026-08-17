using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerTeamRegistrationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerTeamRegistrations",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTeamRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTeamRegistrations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "Club12",
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTeamRegistrations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTeamRegistrations_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "Club12",
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamRegistration_CreatedAt",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamRegistrations_PlayerId_TournamentId",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                columns: new[] { "PlayerId", "TournamentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamRegistrations_TeamId",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamRegistrations_TournamentId",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                column: "TournamentId");

            // Data backfill: every existing player's CURRENT team assignment
            // (Players.TeamId) becomes a season-scoped registration for
            // whatever tournament that team is CURRENTLY assigned to
            // (Teams.TournamentId at the time this migration runs). This
            // preserves today's roster attribution exactly as-is — e.g. a
            // player on a team currently pointed at "Apertura Club 12 2026"
            // gets a registration for that same tournament, a player on a
            // team currently pointed at "Femenino Club12 La Vuelta" gets a
            // registration for that one. Players whose team has no current
            // tournament (Teams.TournamentId IS NULL) are skipped: there is
            // no season to attribute them to.
            migrationBuilder.Sql(
                """
                INSERT INTO "Club12"."PlayerTeamRegistrations"
                    ("Id", "PlayerId", "TeamId", "TournamentId", "DateCreated", "CreatedBy")
                SELECT
                    gen_random_uuid(),
                    p."Id",
                    p."TeamId",
                    t."TournamentId",
                    p."DateCreated",
                    'System'
                FROM "Club12"."Players" p
                JOIN "Club12"."Teams" t ON t."Id" = p."TeamId"
                WHERE t."TournamentId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerTeamRegistrations",
                schema: "Club12");
        }
    }
}
