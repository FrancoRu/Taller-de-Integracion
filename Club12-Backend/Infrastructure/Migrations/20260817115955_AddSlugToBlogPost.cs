using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSlugToBlogPost : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Slug",
            schema: "Club12",
            table: "BlogPosts",
            type: "character varying(220)",
            maxLength: 220,
            nullable: true);

        // Backfills every existing post's Slug from its current Title, mirroring
        // SlugGenerator.GenerateSlug (lowercase, accented characters
        // transliterated to their base letter, non-alphanumeric runs collapsed
        // to a single hyphen, leading/trailing hyphens trimmed). Rows whose
        // computed base slug collides get a numeric suffix (-2, -3, ...)
        // ordered by Id, the same disambiguation strategy SlugGenerator uses
        // for newly created rows.
        migrationBuilder.Sql(
            @"
                    WITH base AS (
                        SELECT ""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(lower(""Title""), 'áéíóúüñ', 'aeiouun'),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""BlogPosts""
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""BlogPosts"" bp
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE bp.""Id"" = n.""Id"";
                    "
        );

        migrationBuilder.AlterColumn<string>(
            name: "Slug",
            schema: "Club12",
            table: "BlogPosts",
            type: "character varying(220)",
            maxLength: 220,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(220)",
            oldMaxLength: 220,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_BlogPosts_Slug",
            schema: "Club12",
            table: "BlogPosts",
            column: "Slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_BlogPosts_Slug",
            schema: "Club12",
            table: "BlogPosts");

        migrationBuilder.DropColumn(
            name: "Slug",
            schema: "Club12",
            table: "BlogPosts");
    }
}
