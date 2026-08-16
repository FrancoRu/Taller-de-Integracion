using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations;

/// <inheritdoc />
public partial class AddUniqueNameAndDivisionIdInsideStageTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "CONSTRAINT_UNIQUE_STAGE_NAME_AND_DIVISIONID",
            schema: "Club12",
            table: "Stages",
            columns: new[] { "Name", "DivisionId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "CONSTRAINT_UNIQUE_STAGE_NAME_AND_DIVISIONID",
            schema: "Club12",
            table: "Stages");
    }
}
