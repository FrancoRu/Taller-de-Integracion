using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class AddContraintToUniqueDocumentNumberInsidePlayerTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Category",
            schema: "Club12",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "Club",
            schema: "Club12",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "IsFederated",
            schema: "Club12",
            table: "Players");

        migrationBuilder.RenameColumn(
            name: "Names",
            schema: "Club12",
            table: "Players",
            newName: "FirstName");

        migrationBuilder.AddColumn<string>(
            name: "SecondName",
            schema: "Club12",
            table: "Players",
            type: "character varying(70)",
            maxLength: 70,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Players_DocumentNumber",
            schema: "Club12",
            table: "Players",
            column: "DocumentNumber",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Players_DocumentNumber",
            schema: "Club12",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "SecondName",
            schema: "Club12",
            table: "Players");

        migrationBuilder.RenameColumn(
            name: "FirstName",
            schema: "Club12",
            table: "Players",
            newName: "Names");

        migrationBuilder.AddColumn<string>(
            name: "Category",
            schema: "Club12",
            table: "Players",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Club",
            schema: "Club12",
            table: "Players",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsFederated",
            schema: "Club12",
            table: "Players",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
