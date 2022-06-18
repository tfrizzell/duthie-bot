using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class NewsTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastNewsItem",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNewsItem",
                table: "LeagueStates");
        }
    }
}
