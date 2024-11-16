using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesAndAddVenues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Value",
                schema: "Club12",
                table: "PlayersStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<int>(
                name: "MatchWeek",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VenueId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Venues",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_VenueId",
                schema: "Club12",
                table: "Matches",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Venues_VenueId",
                schema: "Club12",
                table: "Matches",
                column: "VenueId",
                principalSchema: "Club12",
                principalTable: "Venues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Venues_VenueId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "Venues",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_Matches_VenueId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MatchWeek",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "VenueId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<double>(
                name: "Value",
                schema: "Club12",
                table: "PlayersStatistics",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
