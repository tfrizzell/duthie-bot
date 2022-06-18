using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class DailyStarTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Message",
                table: "GuildMessages",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "Embed",
                table: "GuildMessages",
                newName: "Url");

            migrationBuilder.AddColumn<string>(
                name: "LastDailyStar",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "Color",
                table: "GuildMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Footer",
                table: "GuildMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "GuildMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timestamp",
                table: "GuildMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "GuildMessages",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDailyStar",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "GuildMessages");

            migrationBuilder.DropColumn(
                name: "Footer",
                table: "GuildMessages");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "GuildMessages");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "GuildMessages");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "GuildMessages");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "GuildMessages",
                newName: "Embed");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "GuildMessages",
                newName: "Message");
        }
    }
}
