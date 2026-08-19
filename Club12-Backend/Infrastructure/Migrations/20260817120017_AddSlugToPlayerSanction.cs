using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSlugToPlayerSanction : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Slug",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "character varying(220)",
            maxLength: 220,
            nullable: true);

        // Backfills every existing sanction's Slug from its sanctioned player's
        // name and the sanction's issued date, mirroring PlayerSanctionService's
        // "{playerFullName} {issuedDate:yyyy-MM-dd}" source string fed through
        // SlugGenerator.GenerateSlug (lowercase, accented characters
        // transliterated to their base letter, non-alphanumeric runs collapsed
        // to a single hyphen, leading/trailing hyphens trimmed). The player's
        // last/first/second name are concatenated the same way as the
        // application's Player.FullName computed property (case does not
        // matter here since the whole string is lowercased regardless). Rows
        // whose computed base slug collides (e.g. the same player sanctioned
        // twice on the same day) get a numeric suffix (-2, -3, ...) ordered by
        // Id, the same disambiguation strategy SlugGenerator uses for newly
        // created rows.
        migrationBuilder.Sql(
            @"
                    WITH base AS (
                        SELECT ps.""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(
                                        lower(
                                            concat(
                                                p.""LastName"", ' ', p.""FirstName"",
                                                CASE WHEN p.""SecondName"" IS NULL OR trim(p.""SecondName"") = '' THEN '' ELSE ' ' || p.""SecondName"" END,
                                                ' ', to_char(ps.""IssuedDate"", 'YYYY-MM-DD')
                                            )
                                        ),
                                        'áéíóúüñ', 'aeiouun'
                                    ),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""PlayerSanctions"" ps
                        JOIN ""Club12"".""Players"" p ON p.""Id"" = ps.""PlayerId""
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""PlayerSanctions"" ps
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE ps.""Id"" = n.""Id"";
                    "
        );

        migrationBuilder.AlterColumn<string>(
            name: "Slug",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "character varying(220)",
            maxLength: 220,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(220)",
            oldMaxLength: 220,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PlayerSanctions_Slug",
            schema: "Club12",
            table: "PlayerSanctions",
            column: "Slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PlayerSanctions_Slug",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "Slug",
            schema: "Club12",
            table: "PlayerSanctions");
    }
}
