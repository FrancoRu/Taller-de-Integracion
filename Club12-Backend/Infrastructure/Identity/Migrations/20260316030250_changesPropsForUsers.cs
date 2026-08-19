using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Infrastructure.Identity.Migrations;

/// <inheritdoc />
public partial class changesPropsForUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CreatedByOwnerId",
            table: "AspNetUsers",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedByOwnerId",
            table: "AspNetUsers");
    }
}
