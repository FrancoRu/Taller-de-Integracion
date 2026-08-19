using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class AddChangesInTournamentEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsFinished",
            schema: "Club12",
            table: "Tournaments",
            type: "boolean",
            nullable: false,
            defaultValue: false);

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

        migrationBuilder.AddColumn<DateTime>(
            name: "StartDate",
            schema: "Club12",
            table: "Tournaments",
            type: "timestamp without time zone",
            nullable: false,
            defaultValue: new DateTime(2, 2, 2, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<DateTime>(
            name: "TeamRegistrationDeadline",
            schema: "Club12",
            table: "Tournaments",
            type: "timestamp without time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<Guid>(
            name: "TournamentId",
            schema: "Club12",
            table: "Teams",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Tournament_DeadlineBeforeStart",
            schema: "Club12",
            table: "Tournaments",
            sql: "\"TeamRegistrationDeadline\" < \"StartDate\"");

        migrationBuilder.CreateIndex(
            name: "IX_Teams_TournamentId",
            schema: "Club12",
            table: "Teams",
            column: "TournamentId");

        migrationBuilder.AddForeignKey(
            name: "FK_Teams_Tournaments_TournamentId",
            schema: "Club12",
            table: "Teams",
            column: "TournamentId",
            principalSchema: "Club12",
            principalTable: "Tournaments",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Teams_Tournaments_TournamentId",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Tournament_DeadlineBeforeStart",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropIndex(
            name: "IX_Teams_TournamentId",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropColumn(
            name: "IsFinished",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "MaxTeams",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "MinTeams",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "StartDate",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "TeamRegistrationDeadline",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "TournamentId",
            schema: "Club12",
            table: "Teams");
    }
}
