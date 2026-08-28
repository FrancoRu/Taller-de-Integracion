using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HU-63/HU-65: the matchday (jornada) a match belongs to, 1-based.
            // Nullable: existing rows and non-round-robin matches (e.g. knockout
            // stages) have no round. New round-robin fixtures set it explicitly.
            migrationBuilder.AddColumn<int>(
                name: "Round",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Round",
                schema: "Club12",
                table: "Matches");
        }
    }
}
