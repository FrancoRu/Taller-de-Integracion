using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAndPlayerFixesAlsoBlogPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShirtColor",
                schema: "Club12",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Names",
                schema: "Club12",
                table: "Players",
                type: "character varying(70)",
                maxLength: 70,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                schema: "Club12",
                table: "Players",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "Club12",
                table: "Players",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SocialSecurity",
                schema: "Club12",
                table: "Players",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                @"UPDATE ""Club12"".""Players""
                  SET ""Names"" = CONCAT(COALESCE(""FirstName"", ''), ' ', COALESCE(""SecondName"", ''))
                  WHERE ""FirstName"" IS NOT NULL OR ""SecondName"" IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SecondName",
                schema: "Club12",
                table: "Players");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                schema: "Club12",
                table: "Players",
                type: "character varying(70)",
                maxLength: 70,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(35)",
                oldMaxLength: 35);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                schema: "Club12",
                table: "Players",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Author = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MarkdownText = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                schema: "Club12",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Names = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TeamId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Staff_Teams_TeamId1",
                        column: x => x.TeamId1,
                        principalSchema: "Club12",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TeamId",
                schema: "Club12",
                table: "Staff",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TeamId1",
                schema: "Club12",
                table: "Staff",
                column: "TeamId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts",
                schema: "Content");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "Club12");

            migrationBuilder.DropColumn(
                name: "ShirtColor",
                schema: "Club12",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                schema: "Club12",
                table: "Players");

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

            migrationBuilder.DropColumn(
                name: "Names",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "Club12",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SocialSecurity",
                schema: "Club12",
                table: "Players");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                schema: "Club12",
                table: "Players",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(70)",
                oldMaxLength: 70);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                schema: "Club12",
                table: "Players",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Club12",
                table: "Players",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondName",
                schema: "Club12",
                table: "Players",
                type: "character varying(35)",
                maxLength: 35,
                nullable: false,
                defaultValue: "");
        }
    }
}
