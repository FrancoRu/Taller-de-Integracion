using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSlugToMatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Slug",
            schema: "Club12",
            table: "Matches",
            type: "character varying(220)",
            maxLength: 220,
            nullable: true);

        // Backfills every existing match's Slug from its home/visitor team names
        // and match date, mirroring MatchSlugSourceBuilder.Build + SlugGenerator.GenerateSlug
        // ("{homeTeamName} vs {visitorTeamName} {yyyy-MM-dd}", lowercased, accented
        // characters transliterated to their base letter, non-alphanumeric runs
        // collapsed to a single hyphen, leading/trailing hyphens trimmed). A team
        // missing from either side (unseeded match) falls back to the same "TBD"
        // placeholder the application uses. Rows whose computed base slug collides
        // (e.g. two legs between the same pairing on the same day, or several
        // unseeded matches) get a numeric suffix (-2, -3, ...) ordered by Id, the
        // same disambiguation strategy SlugGenerator uses for newly created rows.
        migrationBuilder.Sql(
            @"
                    WITH base AS (
                        SELECT m.""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(
                                        lower(
                                            concat(
                                                COALESCE(ht.""Name"", 'TBD'), ' vs ', COALESCE(vt.""Name"", 'TBD'),
                                                ' ', to_char(m.""MatchDate"", 'YYYY-MM-DD')
                                            )
                                        ),
                                        'áéíóúüñ', 'aeiouun'
                                    ),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""Matches"" m
                        LEFT JOIN ""Club12"".""Teams"" ht ON ht.""Id"" = m.""HomeTeamId""
                        LEFT JOIN ""Club12"".""Teams"" vt ON vt.""Id"" = m.""VisitorTeamId""
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""Matches"" m
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE m.""Id"" = n.""Id"";
                    "
        );

        migrationBuilder.AlterColumn<string>(
            name: "Slug",
            schema: "Club12",
            table: "Matches",
            type: "character varying(220)",
            maxLength: 220,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(220)",
            oldMaxLength: 220,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Slug",
            schema: "Club12",
            table: "Matches",
            column: "Slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Matches_Slug",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "Slug",
            schema: "Club12",
            table: "Matches");
    }
}
