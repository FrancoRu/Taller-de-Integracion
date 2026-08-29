using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RebackfillDivisionStageSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-backfills every Division/Stage Slug from its current Name,
            // overwriting the GUID-polluted slugs that the sample seeder used to
            // produce (e.g. "primera-division-34ce6485-...") with a clean
            // kebab-case slug (e.g. "primera-division"). Mirrors
            // SlugGenerator.GenerateSlug exactly: lowercase, Spanish accents
            // transliterated to their base letter, non-alphanumeric runs
            // collapsed to a single hyphen, leading/trailing hyphens trimmed.
            // Rows whose computed base slug collides get a numeric suffix
            // (-2, -3, ...) ordered by Id — the same disambiguation strategy the
            // application and the original 20260828003816 backfill use, so the
            // Slug unique index is preserved.
            migrationBuilder.Sql(
                @"
                    WITH base AS (
                        SELECT ""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(lower(""Name""), 'áéíóúüñ', 'aeiouun'),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""Divisions""
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""Divisions"" t
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE t.""Id"" = n.""Id"";
                    "
            );

            migrationBuilder.Sql(
                @"
                    WITH base AS (
                        SELECT ""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(lower(""Name""), 'áéíóúüñ', 'aeiouun'),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""Stages""
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""Stages"" t
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE t.""Id"" = n.""Id"";
                    "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only re-backfill: there is no meaningful inverse (the original
            // GUID-suffixed slugs are not recoverable). Down is intentionally a
            // no-op so the migration can still be rolled back structurally.
        }
    }
}
