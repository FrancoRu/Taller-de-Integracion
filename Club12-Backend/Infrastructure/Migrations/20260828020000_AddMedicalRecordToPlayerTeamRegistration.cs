using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Infrastructure.Migrations;

/// <summary>
/// Adds the medical-record / eligibility fields to PlayerTeamRegistration
/// (HU-55/57/58/59). Because the record lives on the season registration, it is
/// inherently per season: every existing row starts (and every new row
/// defaults to) 'Pending', so no prior approval is ever inherited (HU-59).
/// </summary>
public partial class AddMedicalRecordToPlayerTeamRegistration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Status persisted as the enum name (string), mirroring the other enum
        // columns in this DB. Existing rows default to 'Pending' — no season
        // ever inherits another season's approval (HU-59).
        migrationBuilder.AddColumn<string>(
            name: "MedicalRecordStatus",
            schema: "Club12",
            table: "PlayerTeamRegistrations",
            type: "text",
            nullable: false,
            defaultValue: "Pending");

        migrationBuilder.AddColumn<string>(
            name: "MedicalRecordFileUrl",
            schema: "Club12",
            table: "PlayerTeamRegistrations",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MedicalRecordFileName",
            schema: "Club12",
            table: "PlayerTeamRegistrations",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MedicalRecordReviewReason",
            schema: "Club12",
            table: "PlayerTeamRegistrations",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "MedicalRecordReviewedAt",
            schema: "Club12",
            table: "PlayerTeamRegistrations",
            type: "timestamp without time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MedicalRecordStatus",
            schema: "Club12",
            table: "PlayerTeamRegistrations");

        migrationBuilder.DropColumn(
            name: "MedicalRecordFileUrl",
            schema: "Club12",
            table: "PlayerTeamRegistrations");

        migrationBuilder.DropColumn(
            name: "MedicalRecordFileName",
            schema: "Club12",
            table: "PlayerTeamRegistrations");

        migrationBuilder.DropColumn(
            name: "MedicalRecordReviewReason",
            schema: "Club12",
            table: "PlayerTeamRegistrations");

        migrationBuilder.DropColumn(
            name: "MedicalRecordReviewedAt",
            schema: "Club12",
            table: "PlayerTeamRegistrations");
    }
}
