using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class LeagueTeamsUniqueKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InternalId",
                table: "LeagueTeams",
                newName: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueTeams_LeagueId_ExternalId",
                table: "LeagueTeams",
                columns: new[] { "LeagueId", "ExternalId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeagueTeams_LeagueId_ExternalId",
                table: "LeagueTeams");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "LeagueTeams",
                newName: "InternalId");
        }
    }
}
