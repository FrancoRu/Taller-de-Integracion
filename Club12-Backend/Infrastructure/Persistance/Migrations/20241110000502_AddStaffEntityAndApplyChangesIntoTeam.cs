using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffEntityAndApplyChangesIntoTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Club12"".""Players""
                SET ""SecondName"" = CONCAT(""FirstName"", ' ', ""SecondName"")
            ");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Club12",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "SecondName",
                schema: "Club12",
                table: "Players",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "ShirtColor",
                schema: "Club12",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClubOrCategory",
                schema: "Club12",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFederated",
                schema: "Club12",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "Club12",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialWork",
                schema: "Club12",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Staffs",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    StaffType = table.Column<string>(type: "text", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_TeamId",
                schema: "Club12",
                table: "Staffs",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Staffs",
                schema: "Club12");

            migrationBuilder.DropColumn(
                name: "ShirtColor",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ClubOrCategory",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsFederated",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SocialWork",
                schema: "Club12",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "Club12",
                table: "Players",
                newName: "SecondName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Club12",
                table: "Players",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                defaultValue: "");
        }
    }
}
