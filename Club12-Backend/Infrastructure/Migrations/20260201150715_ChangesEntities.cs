using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class ChangesEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Divisions_DivisionId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Teams_Divisions_DivisionId",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Tournament_DeadlineBeforeStart",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropIndex(
            name: "IX_Teams_DivisionId",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropIndex(
            name: "IX_Matches_DivisionId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "DivisionId",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropColumn(
            name: "DivisionId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.RenameColumn(
            name: "PlayoffsGenerated",
            schema: "Club12",
            table: "Divisions",
            newName: "CanGenerateStageAutomated");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Tournament_DeadlineBeforeStart",
            schema: "Club12",
            table: "Tournaments",
            sql: "\"TeamRegistrationDeadline\" < \"StartDate\"");

        migrationBuilder.Sql(
            @"
                    UPDATE                 
                    ""Club12"".""Divisions"" 
                    SET                
                    ""CanGenerateStageAutomated"" = CASE
                        WHEN (SELECT COUNT(*) FROM ""Club12"".""Stages"" WHERE ""Stages"".""DivisionId"" = ""Divisions"".""Id"") = 0 THEN TRUE
                        ELSE FALSE
                    END
                    "
            );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Tournament_DeadlineBeforeStart",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.RenameColumn(
            name: "CanGenerateStageAutomated",
            schema: "Club12",
            table: "Divisions",
            newName: "PlayoffsGenerated");

        migrationBuilder.AddColumn<Guid>(
            name: "DivisionId",
            schema: "Club12",
            table: "Teams",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DivisionId",
            schema: "Club12",
            table: "Matches",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Tournament_DeadlineBeforeStart",
            schema: "Club12",
            table: "Tournaments",
            sql: "\"TeamRegistrationDeadline\" < \"StartDate\"");

        migrationBuilder.CreateIndex(
            name: "IX_Teams_DivisionId",
            schema: "Club12",
            table: "Teams",
            column: "DivisionId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_DivisionId",
            schema: "Club12",
            table: "Matches",
            column: "DivisionId");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Divisions_DivisionId",
            schema: "Club12",
            table: "Matches",
            column: "DivisionId",
            principalSchema: "Club12",
            principalTable: "Divisions",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Teams_Divisions_DivisionId",
            schema: "Club12",
            table: "Teams",
            column: "DivisionId",
            principalSchema: "Club12",
            principalTable: "Divisions",
            principalColumn: "Id");
    }
}
