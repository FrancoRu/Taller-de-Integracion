using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClubParentClubId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentClubId",
                schema: "Club12",
                table: "Clubs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ParentClubId",
                schema: "Club12",
                table: "Clubs",
                column: "ParentClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_Clubs_ParentClubId",
                schema: "Club12",
                table: "Clubs",
                column: "ParentClubId",
                principalSchema: "Club12",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_Clubs_ParentClubId",
                schema: "Club12",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_ParentClubId",
                schema: "Club12",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ParentClubId",
                schema: "Club12",
                table: "Clubs");
        }
    }
}
