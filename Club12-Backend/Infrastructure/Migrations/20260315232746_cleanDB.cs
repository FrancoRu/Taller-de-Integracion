using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class cleanDB : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Staffs",
            schema: "Club12");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "Club12");

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Venues",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Venues",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Tournaments",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Tournaments",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Teams",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Teams",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "StageTeamMatches",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "StageTeamMatches",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Stages",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Stages",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "PlayersStatistics",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "PlayersStatistics",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Players",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Players",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Matches",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Matches",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "Divisions",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Divisions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedBy",
            schema: "Club12",
            table: "BlogPosts",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UpdatedBy",
            schema: "Club12",
            table: "BlogPosts",
            type: "text",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Venue_CreatedAt",
            schema: "Club12",
            table: "Venues",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_Tournament_CreatedAt",
            schema: "Club12",
            table: "Tournaments",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_Team_CreatedAt",
            schema: "Club12",
            table: "Teams",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_StageTeamMatch_CreatedAt",
            schema: "Club12",
            table: "StageTeamMatches",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_Stage_CreatedAt",
            schema: "Club12",
            table: "Stages",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerStatistic_CreatedAt",
            schema: "Club12",
            table: "PlayersStatistics",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerSanction_CreatedAt",
            schema: "Club12",
            table: "PlayerSanctions",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_Player_CreatedAt",
            schema: "Club12",
            table: "Players",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_Match_CreatedAt",
            schema: "Club12",
            table: "Matches",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_Division_CreatedAt",
            schema: "Club12",
            table: "Divisions",
            column: "DateCreated");

        migrationBuilder.CreateIndex(
            name: "IX_BlogPost_CreatedAt",
            schema: "Club12",
            table: "BlogPosts",
            column: "DateCreated");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Venue_CreatedAt",
            schema: "Club12",
            table: "Venues");

        migrationBuilder.DropIndex(
            name: "IX_Tournament_CreatedAt",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropIndex(
            name: "IX_Team_CreatedAt",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropIndex(
            name: "IX_StageTeamMatch_CreatedAt",
            schema: "Club12",
            table: "StageTeamMatches");

        migrationBuilder.DropIndex(
            name: "IX_Stage_CreatedAt",
            schema: "Club12",
            table: "Stages");

        migrationBuilder.DropIndex(
            name: "IX_PlayerStatistic_CreatedAt",
            schema: "Club12",
            table: "PlayersStatistics");

        migrationBuilder.DropIndex(
            name: "IX_PlayerSanction_CreatedAt",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropIndex(
            name: "IX_Player_CreatedAt",
            schema: "Club12",
            table: "Players");

        migrationBuilder.DropIndex(
            name: "IX_Match_CreatedAt",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropIndex(
            name: "IX_Division_CreatedAt",
            schema: "Club12",
            table: "Divisions");

        migrationBuilder.DropIndex(
            name: "IX_BlogPost_CreatedAt",
            schema: "Club12",
            table: "BlogPosts");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Venues");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Venues");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Tournaments");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Teams");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "StageTeamMatches");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "StageTeamMatches");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Stages");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Stages");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "PlayersStatistics");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "PlayersStatistics");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "Divisions");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "Divisions");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            schema: "Club12",
            table: "BlogPosts");

        migrationBuilder.DropColumn(
            name: "UpdatedBy",
            schema: "Club12",
            table: "BlogPosts");

        migrationBuilder.CreateTable(
            name: "Staffs",
            schema: "Club12",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Names = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                Type = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Staffs", x => x.Id);
                table.ForeignKey(
                    name: "FK_Staffs_Teams_TeamId",
                    column: x => x.TeamId,
                    principalSchema: "Club12",
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "Club12",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Password = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RefreshToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                RefreshTokenExpiryTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                Username = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Staffs_TeamId",
            schema: "Club12",
            table: "Staffs",
            column: "TeamId");
    }
}
