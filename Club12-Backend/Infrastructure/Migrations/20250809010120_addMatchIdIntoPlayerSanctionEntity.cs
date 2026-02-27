using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class addMatchIdIntoPlayerSanctionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MatchId",
                schema: "Club12",
                table: "PlayerSanctions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_MatchId",
                schema: "Club12",
                table: "PlayerSanctions",
                column: "MatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSanctions_Matches_MatchId",
                schema: "Club12",
                table: "PlayerSanctions",
                column: "MatchId",
                principalSchema: "Club12",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSanctions_Matches_MatchId",
                schema: "Club12",
                table: "PlayerSanctions");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSanctions_MatchId",
                schema: "Club12",
                table: "PlayerSanctions");

            migrationBuilder.DropColumn(
                name: "MatchId",
                schema: "Club12",
                table: "PlayerSanctions");
        }
    }
}
