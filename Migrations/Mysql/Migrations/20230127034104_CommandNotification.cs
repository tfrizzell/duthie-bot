using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Migrations.Mysql.Migrations
{
    public partial class CommandNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CommandNotificationSent",
                table: "Guilds",
                type: "tinyint(1)",
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
