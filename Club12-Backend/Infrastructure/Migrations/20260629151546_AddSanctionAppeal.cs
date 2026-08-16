using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSanctionAppeal : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "AppealDate",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "timestamp without time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AppealReason",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AppealResolution",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "AppealResolvedDate",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "timestamp without time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AppealStatus",
            schema: "Club12",
            table: "PlayerSanctions",
            type: "text",
            nullable: false,
            defaultValue: "None");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AppealDate",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "AppealReason",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "AppealResolution",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "AppealResolvedDate",
            schema: "Club12",
            table: "PlayerSanctions");

        migrationBuilder.DropColumn(
            name: "AppealStatus",
            schema: "Club12",
            table: "PlayerSanctions");
    }
}
