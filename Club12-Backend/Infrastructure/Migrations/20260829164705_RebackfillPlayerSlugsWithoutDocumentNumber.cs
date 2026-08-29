using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RebackfillPlayerSlugsWithoutDocumentNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-backfills every Player.Slug to the single canonical format
            // "apellido-nombre[-segundo]" (Player.BuildSlugSource fed through
            // SlugGenerator.GenerateSlug) — dropping the DocumentNumber segment
            // that the old sample seeder used to append. Mirrors the shipped
            // 20260828003816 player backfill verbatim (same concat, same
            // translate/regexp_replace, same ROW_NUMBER() ... ORDER BY "Id"
            // suffix rule) so create, seed and backfill share one rule.
            //
            // Reversible via a snapshot ledger: pre-migration slugs are an
            // unrecoverable mixture of "apellido-nombre-dni" (seed) and
            // "apellido-nombre[-N]" (create/backfill), so the only correct
            // inverse is to store and restore them. The ledger table lives
            // outside the EF model, so the model differ ignores it.

            // 1. Rollback ledger — drop+recreate (idempotent under a Down->Up
            //    cycle) and snapshot the current slugs.
            migrationBuilder.Sql(
                @"
                    DROP TABLE IF EXISTS ""Club12"".""PlayerSlugBackup_20260829"";

                    CREATE TABLE ""Club12"".""PlayerSlugBackup_20260829"" (
                        ""Id"" uuid PRIMARY KEY,
                        ""OldSlug"" character varying(220) NOT NULL
                    );

                    INSERT INTO ""Club12"".""PlayerSlugBackup_20260829"" (""Id"", ""OldSlug"")
                    SELECT ""Id"", ""Slug"" FROM ""Club12"".""Players"";
                    "
            );

            // 2. Park every slug on a value SlugGenerator's [^a-z0-9]+ rule can
            //    never emit ('_' is disjoint from every real and every final
            //    slug), so the two-phase reassignment below never trips the
            //    plain (non-deferrable) IX_Players_Slug unique index mid-update.
            migrationBuilder.Sql(
                @"
                    UPDATE ""Club12"".""Players"" SET ""Slug"" = '__tmp_' || ""Id""::text;
                    "
            );

            // 3. Assign canonical slugs. Targets are distinct by construction
            //    (ROW_NUMBER suffix) and disjoint from '__tmp_%'.
            migrationBuilder.Sql(
                @"
                    WITH base AS (
                        SELECT p.""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(
                                        lower(
                                            concat(
                                                p.""LastName"", ' ', p.""FirstName"",
                                                CASE WHEN p.""SecondName"" IS NULL OR trim(p.""SecondName"") = '' THEN '' ELSE ' ' || p.""SecondName"" END
                                            )
                                        ),
                                        'áéíóúüñ', 'aeiouun'
                                    ),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""Players"" p
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""Players"" t
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE t.""Id"" = n.""Id"";
                    "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // True inverse, guarded so it never half-restores. Re-park first for
            // the same index-permutation reason as Up. Players inserted AFTER Up
            // have no ledger row and keep their canonical slug — correct, since
            // no prior value exists to restore. Double-Down is a safe no-op.
            migrationBuilder.Sql(
                @"
                    UPDATE ""Club12"".""Players"" SET ""Slug"" = '__tmp_' || ""Id""::text
                    WHERE to_regclass('""Club12"".""PlayerSlugBackup_20260829""') IS NOT NULL;
                    "
            );

            migrationBuilder.Sql(
                @"
                    UPDATE ""Club12"".""Players"" t
                    SET ""Slug"" = b.""OldSlug""
                    FROM ""Club12"".""PlayerSlugBackup_20260829"" b
                    WHERE t.""Id"" = b.""Id"";
                    "
            );

            migrationBuilder.Sql(
                @"
                    DROP TABLE IF EXISTS ""Club12"".""PlayerSlugBackup_20260829"";
                    "
            );
        }
    }
}
