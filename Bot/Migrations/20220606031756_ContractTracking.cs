using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class ContractTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastContract",
                table: "LeagueState",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastContract",
                table: "LeagueState");
        }
    }
}
