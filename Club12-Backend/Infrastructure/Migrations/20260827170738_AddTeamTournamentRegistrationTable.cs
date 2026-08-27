using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamTournamentRegistrationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamTournamentRegistrations",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTournamentRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamTournamentRegistrations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamTournamentRegistrations_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "Club12",
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamTournamentRegistration_CreatedAt",
                schema: "Club12",
                table: "TeamTournamentRegistrations",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTournamentRegistrations_TeamId_TournamentId",
                schema: "Club12",
                table: "TeamTournamentRegistrations",
                columns: new[] { "TeamId", "TournamentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamTournamentRegistrations_TournamentId",
                schema: "Club12",
                table: "TeamTournamentRegistrations",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamTournamentRegistrations",
                schema: "Club12");
        }
    }
}
