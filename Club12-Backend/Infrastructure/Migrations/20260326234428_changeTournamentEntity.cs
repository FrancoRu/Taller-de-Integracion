using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class changeTournamentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFinished",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "Club12",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                schema: "Club12",
                table: "Tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
