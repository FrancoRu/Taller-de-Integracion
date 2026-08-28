using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionPointsAndPlayoffMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsForLoss",
                schema: "Club12",
                table: "Divisions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PointsForWin",
                schema: "Club12",
                table: "Divisions",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "DivisionPlayoffMappings",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPosition = table.Column<int>(type: "integer", nullable: false),
                    ToPosition = table.Column<int>(type: "integer", nullable: false),
                    Destination = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionPlayoffMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DivisionPlayoffMappings_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalSchema: "Club12",
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DivisionPlayoffMapping_CreatedAt",
                schema: "Club12",
                table: "DivisionPlayoffMappings",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionPlayoffMappings_DivisionId",
                schema: "Club12",
                table: "DivisionPlayoffMappings",
                column: "DivisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DivisionPlayoffMappings",
                schema: "Club12");

            migrationBuilder.DropColumn(
                name: "PointsForLoss",
                schema: "Club12",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "PointsForWin",
                schema: "Club12",
                table: "Divisions");
        }
    }
}
