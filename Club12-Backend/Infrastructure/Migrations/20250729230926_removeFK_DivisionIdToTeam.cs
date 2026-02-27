using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class removeFK_DivisionIdToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Seed",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.AlterColumn<Guid>(
                name: "DivisionId",
                schema: "Club12",
                table: "Teams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                schema: "Club12",
                table: "Teams",
                column: "DivisionId",
                principalSchema: "Club12",
                principalTable: "Divisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.AlterColumn<Guid>(
                name: "DivisionId",
                schema: "Club12",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seed",
                schema: "Club12",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                schema: "Club12",
                table: "Teams",
                column: "DivisionId",
                principalSchema: "Club12",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
