using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Migrations.Sqlite.Migrations
{
    public partial class CommandNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CommandNotificationSent",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommandNotificationSent",
                table: "Guilds");
        }
    }
}
