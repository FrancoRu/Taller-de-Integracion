using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HU-69: new result-lifecycle column. New rows default to
            // "Scheduled"; the column is persisted as the enum name (string).
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Club12",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "Scheduled");

            // Backfill existing rows: any already-finished match is a normally
            // played result (walkovers did not exist before this column).
            migrationBuilder.Sql(
                @"UPDATE ""Club12"".""Matches"" SET ""Status"" = 'Played' WHERE ""IsFinished"" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Club12",
                table: "Matches");
        }
    }
}
