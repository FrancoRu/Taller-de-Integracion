using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectToPlayerSanction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HU-77: a sanction subject can now be a player, a team or a staff
            // member. Existing rows are all player sanctions, so SubjectType
            // defaults to 'Player' and PlayerId keeps its value.

            // Drop the NOT NULL constraint on PlayerId: team/staff sanctions
            // have no player.
            migrationBuilder.AlterColumn<System.Guid>(
                name: "PlayerId",
                schema: "Club12",
                table: "PlayerSanctions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(System.Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "SubjectType",
                schema: "Club12",
                table: "PlayerSanctions",
                type: "text",
                nullable: false,
                defaultValue: "Player");

            migrationBuilder.AddColumn<System.Guid>(
                name: "TeamId",
                schema: "Club12",
                table: "PlayerSanctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffName",
                schema: "Club12",
                table: "PlayerSanctions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_TeamId",
                schema: "Club12",
                table: "PlayerSanctions",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSanctions_Teams_TeamId",
                schema: "Club12",
                table: "PlayerSanctions",
                column: "TeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSanctions_Teams_TeamId",
                schema: "Club12",
                table: "PlayerSanctions");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSanctions_TeamId",
                schema: "Club12",
                table: "PlayerSanctions");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                schema: "Club12",
                table: "PlayerSanctions");

            migrationBuilder.DropColumn(
                name: "TeamId",
                schema: "Club12",
                table: "PlayerSanctions");

            migrationBuilder.DropColumn(
                name: "StaffName",
                schema: "Club12",
                table: "PlayerSanctions");

            // Restore NOT NULL on PlayerId. Any team/staff sanctions must be
            // removed before rolling back, otherwise this will fail on their
            // null PlayerId (intentional: they cannot be represented downlevel).
            migrationBuilder.AlterColumn<System.Guid>(
                name: "PlayerId",
                schema: "Club12",
                table: "PlayerSanctions",
                type: "uuid",
                nullable: false,
                defaultValue: System.Guid.Empty,
                oldClrType: typeof(System.Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
