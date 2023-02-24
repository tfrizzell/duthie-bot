using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Migrations.Sqlite.Migrations
{
    public partial class StateTrackingTimestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastWaiver",
                table: "LeagueStates",
                newName: "LastWaiverHash");

            migrationBuilder.RenameColumn(
                name: "LastTrade",
                table: "LeagueStates",
                newName: "LastTradeHash");

            migrationBuilder.RenameColumn(
                name: "LastRosterTransaction",
                table: "LeagueStates",
                newName: "LastRosterTransactionHash");

            migrationBuilder.RenameColumn(
                name: "LastNewsItem",
                table: "LeagueStates",
                newName: "LastNewsItemHash");

            migrationBuilder.RenameColumn(
                name: "LastDraftPick",
                table: "LeagueStates",
                newName: "LastDraftPickHash");

            migrationBuilder.RenameColumn(
                name: "LastDailyStar",
                table: "LeagueStates",
                newName: "LastDailyStarTimestamp");

            migrationBuilder.RenameColumn(
                name: "LastContract",
                table: "LeagueStates",
                newName: "LastContractHash");

            migrationBuilder.RenameColumn(
                name: "LastBid",
                table: "LeagueStates",
                newName: "LastBidHash");

            migrationBuilder.AddColumn<string>(
                name: "LastBidTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastContractTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastDraftPickTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNewsItemTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastRosterTransactionTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastTradeTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastWaiverTimestamp",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastWaiverTimestamp",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "LastTradeTimestamp",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "LastRosterTransactionTimestamp",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "LastNewsItemTimestamp",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "LastDraftPickTimestamp",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "LastContractTimestamp",
                table: "LeagueStates");

            migrationBuilder.DropColumn(
                name: "LastBidTimestamp",
                table: "LeagueStates");

            migrationBuilder.RenameColumn(
                name: "LastWaiverHash",
                table: "LeagueStates",
                newName: "LastWaiver");

            migrationBuilder.RenameColumn(
                name: "LastTradeHash",
                table: "LeagueStates",
                newName: "LastTrade");

            migrationBuilder.RenameColumn(
                name: "LastRosterTransactionHash",
                table: "LeagueStates",
                newName: "LastRosterTransaction");

            migrationBuilder.RenameColumn(
                name: "LastNewsItemHash",
                table: "LeagueStates",
                newName: "LastNewsItem");

            migrationBuilder.RenameColumn(
                name: "LastDraftPickHash",
                table: "LeagueStates",
                newName: "LastDraftPick");

            migrationBuilder.RenameColumn(
                name: "LastDailyStarHash",
                table: "LeagueStates",
                newName: "LastDailyStar");

            migrationBuilder.RenameColumn(
                name: "LastContractHash",
                table: "LeagueStates",
                newName: "LastContract");

            migrationBuilder.RenameColumn(
                name: "LastBidHash",
                table: "LeagueStates",
                newName: "LastBid");
        }
    }
}
