using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTeamApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Coaches");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Partners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Coaches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Athletes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Athletes");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Partners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Coaches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
