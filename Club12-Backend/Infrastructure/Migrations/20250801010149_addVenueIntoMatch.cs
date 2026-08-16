using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class addVenueIntoMatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VenueId",
            schema: "Club12",
            table: "Matches",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Matches_VenueId",
            schema: "Club12",
            table: "Matches",
            column: "VenueId");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Venues_VenueId",
            schema: "Club12",
            table: "Matches",
            column: "VenueId",
            principalSchema: "Club12",
            principalTable: "Venues",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Venues_VenueId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropIndex(
            name: "IX_Matches_VenueId",
            schema: "Club12",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "VenueId",
            schema: "Club12",
            table: "Matches");
    }
}
