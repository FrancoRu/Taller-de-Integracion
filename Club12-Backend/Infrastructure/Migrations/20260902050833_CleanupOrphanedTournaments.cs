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
            // This mirrors TournamentService.DeleteTournamentAsync exactly: the
            // same "safe to delete" guard (not started — Status not in
            // Ongoing/Finished — and no played match), AND the same pre-step of
            // clearing every enrolled team's denormalized Team.TournamentId
            // "current-season" pointer first. That FK (Team -> Tournament) is
            // NoAction by design, so a still-set pointer aborts the delete with
            // an opaque FK error (FK_Teams_Tournaments_TournamentId) — which is
            // exactly what the first cut of this migration hit. Team identities
            // survive; their TeamTournamentRegistration rows, plus the
            // tournament's Divisions/Stages/Matches, cascade away via their
            // existing FK configuration. An orphaned tournament that already has
            // real history is never touched and is left for an admin to handle.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'Club12' AND table_name = 'Tournaments'
                    ) THEN
                        CREATE TEMPORARY TABLE _orphaned_tournaments ON COMMIT DROP AS
                        SELECT t."Id"
                        FROM "Club12"."Tournaments" t
                        WHERE t."SeasonId" IS NULL
                          AND t."Status" NOT IN (3, 4) -- Ongoing, Finished
                          AND NOT EXISTS (
                              SELECT 1
                              FROM "Club12"."Matches" m
                              JOIN "Club12"."Stages" s ON s."Id" = m."StageId"
                              JOIN "Club12"."Divisions" d ON d."Id" = s."DivisionId"
                              WHERE d."TournamentId" = t."Id" AND m."IsFinished" = TRUE
                          );

                        UPDATE "Club12"."Teams"
                        SET "TournamentId" = NULL
                        WHERE "TournamentId" IN (SELECT "Id" FROM _orphaned_tournaments);

                        DELETE FROM "Club12"."Tournaments"
                        WHERE "Id" IN (SELECT "Id" FROM _orphaned_tournaments);
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
