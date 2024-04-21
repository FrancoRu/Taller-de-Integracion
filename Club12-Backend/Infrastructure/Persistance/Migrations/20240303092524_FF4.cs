using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class FF4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_WinningTeamId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "WinningTeamId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "VisitorScore",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HomeScore",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_WinningTeamId",
                schema: "Club12",
                table: "Matches",
                column: "WinningTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_WinningTeamId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "WinningTeamId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VisitorScore",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HomeScore",
                schema: "Club12",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_WinningTeamId",
                schema: "Club12",
                table: "Matches",
                column: "WinningTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
