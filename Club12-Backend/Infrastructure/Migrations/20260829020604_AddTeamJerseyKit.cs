using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamJerseyKit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JerseyStyle",
                schema: "Club12",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "solid");

            migrationBuilder.AddColumn<string>(
                name: "ShirtSecondaryColor",
                schema: "Club12",
                table: "Teams",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JerseyStyle",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ShirtSecondaryColor",
                schema: "Club12",
                table: "Teams");
        }
    }
}
