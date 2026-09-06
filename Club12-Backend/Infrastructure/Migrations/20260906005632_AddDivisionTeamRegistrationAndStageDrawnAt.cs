using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionTeamRegistrationAndStageDrawnAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DrawnAt",
                schema: "Club12",
                table: "Stages",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DivisionTeamRegistrations",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionTeamRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DivisionTeamRegistrations_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalSchema: "Club12",
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DivisionTeamRegistrations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DivisionTeamRegistration_CreatedAt",
                schema: "Club12",
                table: "DivisionTeamRegistrations",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionTeamRegistrations_DivisionId",
                schema: "Club12",
                table: "DivisionTeamRegistrations",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionTeamRegistrations_TeamId_DivisionId",
                schema: "Club12",
                table: "DivisionTeamRegistrations",
                columns: new[] { "TeamId", "DivisionId" },
                unique: true);

            // Backfills one DivisionTeamRegistration row per distinct (TeamId,
            // DivisionId) pair implied by existing StageTeamMatch rows,
            // resolved through Stage.DivisionId. GROUP BY the pair, never
            // TeamId alone, so a team placed in two sub-groups of one
            // division or in a group stage plus a same-division bracket
            // collapses to a single row, while a team placed in its regular
            // division and separately in a cross-division-cup division keeps
            // two distinct rows. The NOT EXISTS guard makes this idempotent,
            // safe to re-run against data it has already processed, which
            // matters if this migration is ever re-applied after a rollback.
            migrationBuilder.Sql(
                @"
                    INSERT INTO ""Club12"".""DivisionTeamRegistrations""
                        (""Id"", ""TeamId"", ""DivisionId"", ""DateCreated"", ""DateUpdated"", ""CreatedBy"", ""UpdatedBy"")
                    SELECT gen_random_uuid(),
                           stm.""TeamId"",
                           s.""DivisionId"",
                           now() AT TIME ZONE 'utc',
                           NULL,
                           'System',
                           NULL
                    FROM ""Club12"".""StageTeamMatches"" stm
                    JOIN ""Club12"".""Stages"" s ON stm.""StageId"" = s.""Id""
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ""Club12"".""DivisionTeamRegistrations"" dtr
                        WHERE dtr.""TeamId"" = stm.""TeamId"" AND dtr.""DivisionId"" = s.""DivisionId""
                    )
                    GROUP BY stm.""TeamId"", s.""DivisionId"";
                    "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DrawnAt",
                schema: "Club12",
                table: "Stages");

            migrationBuilder.DropTable(
                name: "DivisionTeamRegistrations",
                schema: "Club12");
        }
    }
}
