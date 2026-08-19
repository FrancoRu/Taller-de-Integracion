using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class AddStageTeamMatchTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StageTeamMatches",
            schema: "Club12",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StageId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StageTeamMatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_StageTeamMatches_Stages_StageId",
                    column: x => x.StageId,
                    principalSchema: "Club12",
                    principalTable: "Stages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_StageTeamMatches_Teams_TeamId",
                    column: x => x.TeamId,
                    principalSchema: "Club12",
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StageTeamMatches_StageId",
            schema: "Club12",
            table: "StageTeamMatches",
            column: "StageId");

        migrationBuilder.CreateIndex(
            name: "IX_StageTeamMatches_TeamId",
            schema: "Club12",
            table: "StageTeamMatches",
            column: "TeamId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StageTeamMatches",
            schema: "Club12");
    }
}
