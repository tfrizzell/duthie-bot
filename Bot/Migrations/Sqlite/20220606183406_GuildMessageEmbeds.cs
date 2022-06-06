using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class GuildMessageEmbeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeagueState_Leagues_LeagueId",
                table: "LeagueState");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeagueState",
                table: "LeagueState");

            migrationBuilder.RenameTable(
                name: "LeagueState",
                newName: "LeagueStates");

            migrationBuilder.AddColumn<string>(
                name: "Embed",
                table: "GuildMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeagueStates",
                table: "LeagueStates",
                column: "LeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeagueStates_Leagues_LeagueId",
                table: "LeagueStates",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeagueStates_Leagues_LeagueId",
                table: "LeagueStates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeagueStates",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "Embed",
                table: "GuildMessages");

            migrationBuilder.RenameTable(
                name: "LeagueStates",
                newName: "LeagueState");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeagueState",
                table: "LeagueState",
                column: "LeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeagueState_Leagues_LeagueId",
                table: "LeagueState",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
