using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class LeagueAffiliatesUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeagueAffiliates_Leagues_AffiliateLeagueId",
                table: "LeagueAffiliates");

            migrationBuilder.RenameColumn(
                name: "AffiliateLeagueId",
                table: "LeagueAffiliates",
                newName: "AffiliateId");

            migrationBuilder.RenameIndex(
                name: "IX_LeagueAffiliates_AffiliateLeagueId",
                table: "LeagueAffiliates",
                newName: "IX_LeagueAffiliates_AffiliateId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeagueAffiliates_Leagues_AffiliateId",
                table: "LeagueAffiliates",
                column: "AffiliateId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeagueAffiliates_Leagues_AffiliateId",
                table: "LeagueAffiliates");

            migrationBuilder.RenameColumn(
                name: "AffiliateId",
                table: "LeagueAffiliates",
                newName: "AffiliateLeagueId");

            migrationBuilder.RenameIndex(
                name: "IX_LeagueAffiliates_AffiliateId",
                table: "LeagueAffiliates",
                newName: "IX_LeagueAffiliates_AffiliateLeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeagueAffiliates_Leagues_AffiliateLeagueId",
                table: "LeagueAffiliates",
                column: "AffiliateLeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
