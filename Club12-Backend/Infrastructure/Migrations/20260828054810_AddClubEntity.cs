using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClubEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                schema: "Club12",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clubs",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ClubId",
                schema: "Club12",
                table: "Teams",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Club_CreatedAt",
                schema: "Club12",
                table: "Clubs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_Slug",
                schema: "Club12",
                table: "Clubs",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Clubs_ClubId",
                schema: "Club12",
                table: "Teams",
                column: "ClubId",
                principalSchema: "Club12",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Clubs_ClubId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "Clubs",
                schema: "Club12");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ClubId",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ClubId",
                schema: "Club12",
                table: "Teams");
        }
    }
}
