using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddBackupRecordTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BackupRecords",
            schema: "Club12",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Origin = table.Column<string>(type: "text", nullable: false),
                DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BackupRecords", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BackupRecord_CreatedAt",
            schema: "Club12",
            table: "BackupRecords",
            column: "DateCreated");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BackupRecords",
            schema: "Club12");
    }
}
