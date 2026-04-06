using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddScorerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scorers",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scorers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scorers_Matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "Club12",
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scorers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "Club12",
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scorer_CreatedAt",
                schema: "Club12",
                table: "Scorers",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Scorers_MatchId",
                schema: "Club12",
                table: "Scorers",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Scorers_PlayerId",
                schema: "Club12",
                table: "Scorers",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scorers",
                schema: "Club12");
        }
    }
}
