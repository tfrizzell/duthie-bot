﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class RosterTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRosterTransaction",
                table: "LeagueStates",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRosterTransaction",
                table: "LeagueStates");
        }
    }
}
