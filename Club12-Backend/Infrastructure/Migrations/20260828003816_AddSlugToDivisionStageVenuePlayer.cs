using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugToDivisionStageVenuePlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Divisions",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Stages",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Venues",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Players",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            // Backfills every existing Division/Stage/Venue row's Slug from its
            // current Name, mirroring SlugGenerator.GenerateSlug (lowercase,
            // accented characters transliterated to their base letter,
            // non-alphanumeric runs collapsed to a single hyphen,
            // leading/trailing hyphens trimmed). Rows whose computed base slug
            // collides get a numeric suffix (-2, -3, ...) ordered by Id, the
            // same disambiguation strategy SlugGenerator uses for newly created
            // rows.
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
                        FROM ""Club12"".""Venues""
                    ),
                    numbered AS (
                        SELECT ""Id"", slug_base,
                            ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY ""Id"") AS rn
                        FROM base
                    )
                    UPDATE ""Club12"".""Venues"" t
                    SET ""Slug"" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
                    FROM numbered n
                    WHERE t.""Id"" = n.""Id"";
                    "
            );

            // Backfills every existing player's Slug from their full name,
            // mirroring PlayerService's use of Player.FullName fed through
            // SlugGenerator.GenerateSlug. The last/first/second name are
            // concatenated the same way as the application's Player.FullName
            // computed property (case does not matter here since the whole
            // string is lowercased regardless). Duplicate names get a numeric
            // suffix (-2, -3, ...) ordered by Id, matching SlugGenerator's
            // disambiguation for newly created rows.
            migrationBuilder.Sql(
                @"
                    WITH base AS (
                        SELECT ""Id"",
                            trim(both '-' from
                                regexp_replace(
                                    translate(
                                        lower(
                                            concat(
                                                ""LastName"", ' ', ""FirstName"",
                                                CASE WHEN ""SecondName"" IS NULL OR trim(""SecondName"") = '' THEN '' ELSE ' ' || ""SecondName"" END
                                            )
                                        ),
                                        'áéíóúüñ', 'aeiouun'
                                    ),
                                    '[^a-z0-9]+', '-', 'g'
                                )
                            ) AS slug_base
                        FROM ""Club12"".""Players""
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

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Divisions",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Stages",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Venues",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Club12",
                table: "Players",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_Slug",
                schema: "Club12",
                table: "Divisions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stages_Slug",
                schema: "Club12",
                table: "Stages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_Slug",
                schema: "Club12",
                table: "Venues",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Slug",
                schema: "Club12",
                table: "Players",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venues_Slug",
                schema: "Club12",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Stages_Slug",
                schema: "Club12",
                table: "Stages");

            migrationBuilder.DropIndex(
                name: "IX_Players_Slug",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_Slug",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "Club12",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "Club12",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "Club12",
                table: "Divisions");
        }
    }
}
