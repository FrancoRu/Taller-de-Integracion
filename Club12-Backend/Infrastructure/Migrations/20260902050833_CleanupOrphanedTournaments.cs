using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupOrphanedTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only cleanup: before this fix (see SeasonService.DeleteSeasonAsync),
            // deleting a Season only detached its tournaments (SetNull, not
            // Cascade) instead of deleting them, leaving "orphaned" tournaments
            // behind — SeasonId is null, but the tournament (and its
            // TeamTournamentRegistrations, blocking those teams' own deletion)
            // is still alive and invisible from any season-scoped screen.
            //
            // This mirrors TournamentService.DeleteTournamentAsync's own guard
            // exactly (hasStarted || hasPlayedMatches) so it only ever removes
            // orphans that were genuinely safe to delete — never touching an
            // orphaned tournament that already has real history, which is left
            // for an admin to handle explicitly. Divisions/Stages/Matches/
            // TeamTournamentRegistrations under a deleted tournament cascade
            // away via their existing FK configuration.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'Club12' AND table_name = 'Tournaments'
                    ) THEN
                        DELETE FROM "Club12"."Tournaments" t
                        WHERE t."SeasonId" IS NULL
                          AND t."Status" NOT IN (3, 4) -- Ongoing, Finished
                          AND NOT EXISTS (
                              SELECT 1
                              FROM "Club12"."Matches" m
                              JOIN "Club12"."Stages" s ON s."Id" = m."StageId"
                              JOIN "Club12"."Divisions" d ON d."Id" = s."DivisionId"
                              WHERE d."TournamentId" = t."Id" AND m."IsFinished" = TRUE
                          );
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: the whole point is deleting rows that never should
            // have survived their season's deletion. Nothing to restore.
        }
    }
}
