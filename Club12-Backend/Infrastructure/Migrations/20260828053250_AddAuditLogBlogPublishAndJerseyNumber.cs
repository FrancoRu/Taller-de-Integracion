using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// Additive schema for three stories:
    /// - HU-101: the AuditLogs table (sensitive-action trail).
    /// - HU-16: BlogPosts.IsPublished (draft/published; defaults true so
    ///   existing posts stay publicly visible).
    /// - HU-54: PlayerTeamRegistrations.JerseyNumber (dorsal) plus the unique
    ///   (TeamId, TournamentId, JerseyNumber) index enforcing unique dorsals
    ///   within a team+season. The pre-existing standalone TeamId index is
    ///   dropped because the new composite index covers it as a prefix.
    ///
    /// The HU-77 PlayerSanctions changes (SubjectType/StaffName/TeamId, nullable
    /// PlayerId, the Team FK and its index) are intentionally NOT repeated here:
    /// they were already applied to the database by the hand-authored
    /// 20260828040000_AddSubjectToPlayerSanction migration. This migration only
    /// carries the delta the model snapshot had not yet captured for the three
    /// stories above, so the migration history and the model snapshot converge
    /// without re-adding columns that already exist.
    /// </summary>
    /// <inheritdoc />
    public partial class AddAuditLogBlogPublishAndJerseyNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerTeamRegistrations_TeamId",
                schema: "Club12",
                table: "PlayerTeamRegistrations");

            migrationBuilder.AddColumn<int>(
                name: "JerseyNumber",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "Club12",
                table: "BlogPosts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamRegistrations_TeamId_TournamentId_JerseyNumber",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                columns: new[] { "TeamId", "TournamentId", "JerseyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CreatedAt",
                schema: "Club12",
                table: "AuditLogs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                schema: "Club12",
                table: "AuditLogs",
                column: "Action");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTeamRegistrations_TeamId_TournamentId_JerseyNumber",
                schema: "Club12",
                table: "PlayerTeamRegistrations");

            migrationBuilder.DropColumn(
                name: "JerseyNumber",
                schema: "Club12",
                table: "PlayerTeamRegistrations");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "Club12",
                table: "BlogPosts");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamRegistrations_TeamId",
                schema: "Club12",
                table: "PlayerTeamRegistrations",
                column: "TeamId");
        }
    }
}
