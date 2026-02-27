using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class removeUnneccessaryProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_VisitorTeamId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CanGenerateStageAutomated",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.AlterColumn<Guid>(
                name: "VisitorTeamId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_VisitorTeamId",
                schema: "Club12",
                table: "Matches",
                column: "VisitorTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_VisitorTeamId",
                schema: "Club12",
                table: "Matches");

            migrationBuilder.AlterColumn<Guid>(
                name: "VisitorTeamId",
                schema: "Club12",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanGenerateStageAutomated",
                schema: "Club12",
                table: "Divisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_VisitorTeamId",
                schema: "Club12",
                table: "Matches",
                column: "VisitorTeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
