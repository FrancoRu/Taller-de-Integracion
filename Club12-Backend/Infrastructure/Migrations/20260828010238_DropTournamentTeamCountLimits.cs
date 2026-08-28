using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTournamentTeamCountLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HU-34: MinTeams / MaxTeams are no longer part of the tournament model.
            migrationBuilder.DropColumn(
                name: "MaxTeams",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "MinTeams",
                schema: "Club12",
                table: "Tournaments");

            // HU-35: the new RegistrationClosed status is inserted BETWEEN
            // OpenForRegistration (1) and Ongoing in the TournamentStatus enum,
            // which is persisted as its integer ordinal. Inserting a value in
            // the middle shifts every following member up by one, so existing
            // rows must be remapped: Ongoing 2->3, Finished 3->4, Canceled 4->5.
            // Updated in descending order so an already-shifted value is never
            // re-matched by a later statement. Scheduled (0) and
            // OpenForRegistration (1) are unchanged; no existing row can hold
            // the newly freed value 2 (RegistrationClosed).
            migrationBuilder.Sql(@"UPDATE ""Club12"".""Tournaments"" SET ""Status"" = 5 WHERE ""Status"" = 4;");
            migrationBuilder.Sql(@"UPDATE ""Club12"".""Tournaments"" SET ""Status"" = 4 WHERE ""Status"" = 3;");
            migrationBuilder.Sql(@"UPDATE ""Club12"".""Tournaments"" SET ""Status"" = 3 WHERE ""Status"" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the status remap (ascending, mirror of Up): Canceled
            // 5->4, Finished 4->3, Ongoing 3->2. Any row left on 2 after this
            // (a RegistrationClosed tournament) collapses back onto Ongoing (2),
            // the closest pre-RegistrationClosed state.
            migrationBuilder.Sql(@"UPDATE ""Club12"".""Tournaments"" SET ""Status"" = 2 WHERE ""Status"" = 3;");
            migrationBuilder.Sql(@"UPDATE ""Club12"".""Tournaments"" SET ""Status"" = 3 WHERE ""Status"" = 4;");
            migrationBuilder.Sql(@"UPDATE ""Club12"".""Tournaments"" SET ""Status"" = 4 WHERE ""Status"" = 5;");

            migrationBuilder.AddColumn<int>(
                name: "MaxTeams",
                schema: "Club12",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinTeams",
                schema: "Club12",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
