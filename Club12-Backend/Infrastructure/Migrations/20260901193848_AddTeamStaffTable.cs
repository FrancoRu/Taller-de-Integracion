using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamStaffTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamStaffs",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStaffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamStaffs_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamStaffs_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "Club12",
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaff_CreatedAt",
                schema: "Club12",
                table: "TeamStaffs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaffs_TeamId_TournamentId",
                schema: "Club12",
                table: "TeamStaffs",
                columns: new[] { "TeamId", "TournamentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaffs_TournamentId",
                schema: "Club12",
                table: "TeamStaffs",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamStaffs",
                schema: "Club12");
        }
    }
}
