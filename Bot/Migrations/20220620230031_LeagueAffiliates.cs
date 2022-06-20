using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class LeagueAffiliates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeagueAffiliates",
                columns: table => new
                {
                    LeagueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AffiliateLeagueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueAffiliates", x => new { x.LeagueId, x.AffiliateLeagueId });
                    table.ForeignKey(
                        name: "FK_LeagueAffiliates_Leagues_AffiliateLeagueId",
                        column: x => x.AffiliateLeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueAffiliates_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueAffiliates_AffiliateLeagueId",
                table: "LeagueAffiliates",
                column: "AffiliateLeagueId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueAffiliates");
        }
    }
}
