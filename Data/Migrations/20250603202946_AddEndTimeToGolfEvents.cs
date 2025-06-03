using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTeamApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEndTimeToGolfEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "GolfEvents",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "GolfEvents");
        }
    }
}
