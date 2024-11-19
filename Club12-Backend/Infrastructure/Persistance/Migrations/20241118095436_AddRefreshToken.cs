using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Teams_TeamId",
                schema: "Club12",
                table: "Staff");

            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Teams_TeamId1",
                schema: "Club12",
                table: "Staff");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Staff",
                schema: "Club12",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_TeamId1",
                schema: "Club12",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "TeamId1",
                schema: "Club12",
                table: "Staff");

            migrationBuilder.RenameTable(
                name: "Staff",
                schema: "Club12",
                newName: "Staffs",
                newSchema: "Club12");

            migrationBuilder.RenameIndex(
                name: "IX_Staff_TeamId",
                schema: "Club12",
                table: "Staffs",
                newName: "IX_Staffs_TeamId");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                schema: "Club12",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                schema: "Club12",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Staffs",
                schema: "Club12",
                table: "Staffs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_Teams_TeamId",
                schema: "Club12",
                table: "Staffs",
                column: "TeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_Teams_TeamId",
                schema: "Club12",
                table: "Staffs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Staffs",
                schema: "Club12",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                schema: "Club12",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                schema: "Club12",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Staffs",
                schema: "Club12",
                newName: "Staff",
                newSchema: "Club12");

            migrationBuilder.RenameIndex(
                name: "IX_Staffs_TeamId",
                schema: "Club12",
                table: "Staff",
                newName: "IX_Staff_TeamId");

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId1",
                schema: "Club12",
                table: "Staff",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Staff",
                schema: "Club12",
                table: "Staff",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TeamId1",
                schema: "Club12",
                table: "Staff",
                column: "TeamId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Teams_TeamId",
                schema: "Club12",
                table: "Staff",
                column: "TeamId",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Teams_TeamId1",
                schema: "Club12",
                table: "Staff",
                column: "TeamId1",
                principalSchema: "Club12",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
