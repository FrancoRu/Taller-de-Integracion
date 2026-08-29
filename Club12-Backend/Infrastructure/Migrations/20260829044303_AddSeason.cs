using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeasonId",
                schema: "Club12",
                table: "Tournaments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Seasons",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_SeasonId",
                schema: "Club12",
                table: "Tournaments",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Season_CreatedAt",
                schema: "Club12",
                table: "Seasons",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Slug",
                schema: "Club12",
                table: "Seasons",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Seasons_SeasonId",
                schema: "Club12",
                table: "Tournaments",
                column: "SeasonId",
                principalSchema: "Club12",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Seasons_SeasonId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "Seasons",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_SeasonId",
                schema: "Club12",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                schema: "Club12",
                table: "Tournaments");
        }
    }
}
