using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Duthie.Bot.Migrations.Sqlite
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildAdmins",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "TEXT", nullable: false),
                    MemberId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildAdmins", x => new { x.GuildId, x.MemberId });
                    table.ForeignKey(
                        name: "FK_GuildAdmins_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Info = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leagues_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueTeams",
                columns: table => new
                {
                    LeagueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InternalId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueTeams", x => new { x.LeagueId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_LeagueTeams_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Watchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GuildId = table.Column<string>(type: "TEXT", nullable: false),
                    LeagueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ArchivedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchers_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Watchers_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Watchers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("006f5b08-ec66-4f56-b902-15a2425e19a1"), "New Jersey Devils", "Devils", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("02da4cf8-bfd5-4bd1-8c28-8bcaf27910f4"), "Orlando Magic", "Magic", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("02e4284b-223f-45f2-9211-260a7227fe4d"), "Washington Wizards", "Wizards", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("0306f07b-4c19-4961-a47a-cc18d180d34e"), "Medicine Hat Tigers", "Tigers", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("07f0f89d-5dd5-46c5-ba56-5d49baa5c963"), "Rimouski Oceanic", "Oceanic", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("08546050-4b0c-42c6-ac21-27706bc3dcb7"), "Sudbury Wolves", "Wolves", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("09f6fd9b-50dd-4fe3-b639-49a15316d080"), "St. Louis Blues", "Blues", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("0bfff029-610c-4204-b798-1f1d4410e149"), "Florida Panthers", "Panthers", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("11efffb9-17fd-4803-ad7d-dd17f269b400"), "Miami Heat", "Heat", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("149a27a9-a3f0-4f56-a1fd-b37d2f4a6915"), "Calgary Hitmen", "Hitmen", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("163f5dde-842d-4179-afbc-6248f5d65a49"), "London Knights", "Knights", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("17f767e0-4558-40da-a8e5-24d8ecd4a7b1"), "Edmonton Oilers", "Oilers", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("18518127-7ce5-4c65-aeda-b9afa67a101d"), "Brandon Wheat Kings", "Wheat Kings", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("19cae9b1-f408-46ed-b02d-19ef9116f036"), "Houston Rockets", "Rockets", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("1a9718d4-44df-4fe6-9375-686bc46e4de7"), "Saskatoon Blades", "Blades", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("1da163da-3070-4a24-97ee-de76fa506419"), "Anaheim Ducks", "Ducks", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("20682d4f-fc2a-4d04-99a5-77f6664ccc08"), "Rouyn-Noranda Huskies", "Huskies", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("21c164ab-94fa-4d5c-a772-8593e11d3368"), "Victoria Royals", "Royals", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("225a6082-b4d2-4b96-a50f-438a35613f57"), "Victoriaville Tigres", "Tigres", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("23b2b2e8-0491-426b-aa58-e24be766a238"), "New York Islanders", "Islanders", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("24bf4f45-9e6e-4265-8d84-19c1e61ebbee"), "Kingston Frontenacs", "Frontenacs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("281dd704-3504-4ecc-a8c3-3c7d9eeecdd1"), "Charlotte Hornets", "Hornets", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("28533059-5482-4619-95fc-886d414bb877"), "Lethbridge Hurricanes", "Hurricanes", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("28df03d3-b656-4b70-9a4e-353672d1d601"), "Minnesota Wild", "Wild", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("2bf4aef5-6341-46e4-a257-7b7cb1db1c34"), "Ottawa Senators", "Senators", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("2dd56d87-a8ec-4ea8-b883-6a90ff858a25"), "Tucson Roadrunners", "Roadrunners", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("2e4ab086-ac90-4f0d-92c2-3530b6b7745f"), "Memphis Grizzlies", "Grizzlies", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("3414e7a8-378b-495f-b481-004a8aa645a3"), "Vegas Golden Knights", "Golden Knights", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("352b3da6-94c4-413f-8029-c8c34834db3e"), "Washington Capitals", "Capitals", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("3dedbfb1-4c71-44fb-993d-afa3612f222f"), "Spokane Chiefs", "Chiefs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("3eec1694-fe79-47a8-996d-44cdde62542f"), "New York Rangers", "Rangers", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("424dd7bf-4e38-4d44-83e1-2750e16fc0d8"), "Los Angeles Kings", "Kings", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("43c2517f-7900-49a2-8edc-623e223b5361"), "Belleville Senators", "Senators", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("44bd4000-006c-4d32-9b15-ec4299ba18f5"), "Prince Albert Raiders", "Raiders", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("468b6def-be09-41f3-b86c-45eec027f4cc"), "Carolina Hurricanes", "Hurricanes", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("4745cb89-39e1-4909-976b-feba2b833165"), "Colorado Avalanche", "Avalanche", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("47d26c47-feec-45a9-9753-341622ca00af"), "Milwaukee Bucks", "Bucks", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("4902b18b-e630-43c6-87c3-1370593797e5"), "Seattle Kraken", "Kraken", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("49fa5713-8a30-4682-9ff3-ce60aa3cbb42"), "Boston Bruins", "Bruins", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("4c92af9b-b9ee-4e50-866c-55682516f9c4"), "Philadelphia 76ers", "76ers", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("4ce0628a-948a-4bb5-ba7f-bb825730433a"), "San Antonio Rampage", "Rampage", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("4de59986-489b-4f51-95e9-8e7c8266d7ac"), "Hartford Wolf Pack", "Wolf Pack", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("4fc86f23-26b9-477c-811c-219a11abc3a9"), "Baie-Comeau Drakkar", "Brakkar", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("511f0e1c-4a95-44ed-81f5-c86ec1708960"), "Hershey Bears", "Bears", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("52880339-641a-4fbd-a5f2-b1d1e615b0c7"), "Kootenay Ice", "Ice", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("54c2f7a1-28b1-45ca-b2ba-7441259840f2"), "Columbus Blue Jackets", "Blue Jackets", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("5551b8e1-700b-450c-9f44-f29209763b13"), "Providence Bruins", "Bruins", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("596fd2e8-9814-4973-b880-0b38e2e2d850"), "Niagara IceDogs", "IceDogs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("597ba0e7-3097-4e34-8e6b-80d9b2c5b9c6"), "Winnipeg Jets", "Jets", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("5b158251-1a3e-40ef-8a5a-d9e6cbd146b1"), "Chicoutimi Sagueneens", "Sagueneens", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("5f445e64-b7bf-4d79-b248-b431c541e2ba"), "Saginaw Spirit", "Spirit", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("5f58a1f4-d7b5-4d57-a6dd-47b7aaf19dce"), "Cleveland Cavaliers", "Cavaliers", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("60ab0cd0-a54b-48b9-8ec8-082029d5ba92"), "Boston Celtics", "Celtics", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("61646327-282a-470a-bf41-7a6cd5d0569b"), "Moncton Wildcats", "Wildcats", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("61742d85-6a7b-4b24-8bfa-48db06077bf5"), "Swift Current Broncos", "Broncos", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("6299659a-170c-4420-856d-f3af93ccfdce"), "Detroit Red Wings", "Red Wings", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("63d33eeb-14ba-40ff-b09d-d84076485480"), "Red Deer Rebels", "Rebels", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("63dd296b-55d9-4da3-8207-d2444f20ed4a"), "Everett Silvertips", "Silvertips", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("66a99a9d-d3f2-4825-b543-1fc20d4142b6"), "Minnesota Timberwolves", "Timberwolves", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("6822d814-d1f4-4db9-a008-32a8d6621ed9"), "Golden State Warriors", "Warriors", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("6ac9e6a0-c4d6-4d8e-864e-4a4dd2e3917a"), "Acadie-Bathurst Titan", "Titan", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("6ca440a2-b726-42aa-a461-73e5e32857a3"), "Dallas Stars", "Stars", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("6da73452-a860-495a-b987-0d8fce00ef1e"), "Kelowna Rockets", "Rockets", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("74037536-7225-4d5b-bb11-7079fb404ff2"), "Kamloops Blazers", "Blazers", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("74e45ad2-ea56-4cb8-97d8-f7928a384bfb"), "Philadelphia Flyers", "Flyers", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("75d52ec1-8b80-4a42-9962-6fba06029d1c"), "Iowa Wild", "Wild", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("77dc17e2-89ac-4092-ba13-7005adf6335e"), "Oklahoma City Thunder", "Thunder", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("7820614b-b216-4c7a-8cd4-4b29d3836e99"), "Windsor Spitfires", "Spitfires", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("7a457408-ade3-4cdf-865b-e2144bfb8b44"), "Wilkes-Barre/Scranton Penguins", "Penguins", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("7c6db8a5-5b6d-4af2-bdab-33b254d37d98"), "San Diego Gulls", "Gulls", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("7d8c67df-1c02-46aa-a263-a950fe3aeef0"), "Drummondville Voltigeurs", "Voltigeurs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("7e46b1ae-4614-4823-ad9a-d26dfd291daf"), "Los Angeles Lakers", "Lakers", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("80a5ca3b-94d6-462d-9c2b-68d8ff74f1c8"), "Sault Ste. Marie Greyhounds", "Greyhounds", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("80ae8034-d663-4394-809a-09f091447b09"), "Grand Rapids Griffins", "Griffins", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8351c7ab-5bc4-4d40-991f-696f523ee587"), "Toronto Maple Leafs", "Maple Leafs", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("83b2f888-0080-4b73-8b9e-5accd632f618"), "San Jose Barracuda", "Barracuda", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8535d4c7-c465-4cb7-9973-152370ab0941"), "Quebec Remparts", "Remparts", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("86027d33-0d79-4b8c-9a2c-a8a9c1b86c25"), "New York Knicks", "Knicks", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("862a6dcf-5453-4ace-b6a3-b9cd9f573cf7"), "San Antonio Spurs", "Spurs", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("865dc29c-6b17-4497-9765-492382b905b3"), "Cleveland Monsters", "Monsters", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("87a32ee3-bacb-4bb0-9a82-df1c6b25d0ea"), "Barrie Colts", "Colts", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("896b1080-1eb6-48ef-8e74-404594701c2f"), "Laval Rocket", "Rocket", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("89787e32-e720-412c-b0c0-4b44af36b2c7"), "Tri City Americans", "Americans", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("89a261e3-d987-4590-87fa-c0e7f46adc2f"), "Charlotte Checkers", "Checkers", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8a0bb7dd-8f8c-475c-a47f-95836b613983"), "Guelph Storm", "Storm", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8beace9c-fed4-45f7-bcd4-bf3fe14f522c"), "Montreal Canadiens", "Canadiens", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8e054ad9-2892-41d2-a648-4ca6a2ef2d04"), "Charlottetown Islanders", "Islanders", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8ec0071f-209c-4430-ae52-8bb05b4ef403"), "Coachella Valley Firebirds", "Firebirds", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("8ffb943c-c7c9-430a-a1af-ba6a5606e96e"), "Utica Comets", "Comets", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("90cd276e-8445-4e36-a9e2-2a3c0355f196"), "Rochester Americans", "Americans", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("928acb7f-3914-4fd3-aa64-3b9b19636eca"), "Saint John Sea Dogs", "Sea Dogs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("92bdd5df-5b5f-48d9-8f75-5385608b03c4"), "Sacramento Kings", "Kings", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("92e98e53-b5cd-4085-990f-1fa1a0fccefa"), "Nashville Predators", "Predators", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("95abc7c0-74c3-4bcb-8368-99b3bfc1a28d"), "Calgary Flames", "Flames", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("9c7481d2-9d15-412d-9b4e-68b03cd9e62b"), "Henderson Silver Knights", "Silver Knights", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("9f5264ed-b83a-4695-b160-7f2b3bb5e255"), "Owen Sound Attack", "Attack", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a12932c2-459f-4886-821f-63ac5803726b"), "Abbotsford Canucks", "Canucks", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a1464f78-6508-4d94-8781-11c9b8fcf722"), "Chicago Blackhawks", "Blackhawks", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a3621627-eecc-42c5-8150-b553672ded27"), "Manitoba Moose", "Moose", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a5cad9c3-a407-41d6-a0a0-387def7408bf"), "Hamilton Bulldogs", "Bulldogs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a6d77b28-524a-4317-8694-076086307680"), "Toronto Marlies", "Marlies", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a82a334e-dcea-47f7-83d8-be53817fce96"), "Portland Trail Blazers", "Trail Blazers", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("a8555359-6b1f-4785-b149-5e073df4a253"), "Tampa Bay Lightning", "Lightning", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ab1186ac-bf60-419e-b49f-b516d8c7871e"), "Mississauga Steelheads", "Steelheads", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ae3193c9-8ab2-4591-b80e-f98dcab9e489"), "Vancouver Canucks", "Canucks", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b2286c1d-d71c-4114-9ba0-cded81a12035"), "Cape Breton Screaming Eagles", "Screaming Eagles", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b3cc788f-5c54-4c4b-9fc2-95749ed4913d"), "Chicago Wolves", "Wolves", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b3dd97e5-3f0b-4e9d-bfc0-4c9dacb20f83"), "Ottawa 67's", "67's", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b4c7f188-8a02-4c39-904c-70e977fdd3f4"), "Seattle Thunderbirds", "Thunderbirds", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b683888a-51eb-4c2c-9454-fa495d0e78ce"), "Portland Winterhawks", "Winterhawks", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b7d6f01b-1dea-403a-b3fe-a201e747313c"), "Peterborough Petes", "Petes", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("b8322bf8-a93d-4d84-9cee-4682d54f8f97"), "Colorado Eagles", "Eagles", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("bb1b8c2b-3b67-4b3f-95a0-79c2d37710b0"), "Sarnia Sting", "Sting", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("bc2f3ae4-3031-4693-ac56-3602546cd840"), "Toronto Raptors", "Raptors", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("bcd0f97b-07a8-4fac-853d-2324f1b959ff"), "New Orleans Pelicans", "Pelicans", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("bf0ee353-28c4-47c2-9a60-8538e845a317"), "Springfield Thunderbirds", "Thunderbirds", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("c1131bef-99e7-4e5b-a012-e412e8cc5f00"), "Atlanta Hawks", "Hawks", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("c11a41ff-6fb0-44f9-8e1e-a058eb00b98f"), "Buffalo Sabres", "Sabres", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("c2f4699e-e4d5-4ccc-9c94-e5002733dad7"), "Regina Pats", "Pats", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("c55ce002-3a30-454c-bbba-039ba8baef29"), "Bakersfield Condors", "Condors", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("c5bc2260-bd0d-457c-9023-0b00c49da4e7"), "North Bay Battalion", "Battalion", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("c85f679e-2f97-47c6-9334-e484ae827396"), "Flint Firebirds", "Firebirds", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d0cfa638-7561-4374-ba0e-c3c0baad20e6"), "Indiana Pacers", "Pacers", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d167f677-cda1-461a-b49b-7050b7fda4c5"), "Halifax Mooseheads", "Mooseheads", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d24ced8c-785d-46f0-b719-bf9cddc72937"), "Sherbrooke Phoenix", "Phoenix", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d290cd28-178a-4dcc-9813-954247e9b4e2"), "Stockton Heat", "Heat", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d44a5871-60c8-4d61-8634-b3fa5fb64a0b"), "Utah Jazz", "Jazz", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d45a0944-9fd7-4786-9a09-800fbdeb8a53"), "Edmonton Oil Kings", "Oil Kings", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d47636d9-d3fc-492a-bf29-8d0c9eb85283"), "Gatineau Olympiques", "Olympiques", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d59fa64c-1888-4bef-a19d-d8c2b2ff3bcc"), "Moose Jaw Warriors", "Warriors", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d5b7ea33-e2e1-4560-aebf-ed2cc940ba50"), "Val-d'Or Foreurs", "Foreurs", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("d848e8aa-a7c4-4393-9b6b-98cbaa2bd632"), "Arizona Coyotes", "Coyotes", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("da45a77b-a05e-4b63-92fa-46e7cd72cc7a"), "Phoenix Suns", "Suns", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("dc40e0a0-9953-4770-80c9-ecfa42ee5eaf"), "Denver Nuggets", "Nuggets", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ddc83d01-a516-4dfa-bf30-4db388a2decb"), "Erie Otters", "Otters", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("e22a1261-58ce-4ac2-a770-67aabce957bf"), "Shawinigan Cataractes", "Cataractes", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("e3551df5-8e2e-4a03-8d1d-3aa3a96ad2ac"), "Chicago Bulls", "Bulls", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("e797189d-38ab-4b0e-bc77-0b8606ae1eeb"), "Syracuse Crunch", "Crunch", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ebc5cf8c-de57-4eee-a796-2638557cdd30"), "Oshawa Generals", "Generals", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ebfe66fb-4c49-4406-945a-075376822536"), "Dallas Mavericks", "Mavericks", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ece6e51f-fe5e-4d9b-9a55-cff588fada45"), "Brooklyn Nets", "Nets", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ed188592-8997-42e4-81a9-ce46cdfe9cce"), "Detroit Pistons", "Pistons", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ee35f5dd-3dde-47b0-becf-6333ea259c89"), "Bridgeport Islanders", "Islanders", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f0db239b-8d0a-4f20-96d5-f09bf251ee58"), "Los Angeles Clippers", "Clippers", "[\"nba\",\"basketball\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f2e807ba-f4d5-467e-a35b-4a57cb0013e5"), "Vancouver Giants", "Giants", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f3c00ebd-61c5-4b42-a5f9-94b46b16165d"), "Pittsburgh Penguins", "Penguins", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f49f3a2a-185a-413d-a5d1-a1688a697278"), "Milwaukee Admirals", "Admirals", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f62728f5-10a7-4741-9312-9619585a3a40"), "Lehigh Valley Phantoms", "Phantoms", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f69d0075-2036-49d9-9059-05e94d8adcd6"), "Blainville-Boisbriand Armada", "Armada", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("f9c20cb7-7414-485a-b7f5-a014faf8b221"), "Prince George Cougars", "Cougars", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("fd263adb-51c0-4cc9-ba04-cecf45cc291c"), "Rockford IceHogs", "IceHogs", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("fdf4b0fc-7205-4f55-abfc-f3be840596af"), "Ontario Reign", "Reign", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("fe315ea4-6bcf-49b4-b632-03fae03e4e20"), "Kitchener Rangers", "Rangers", "[\"chl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("fe7232b4-f01c-452b-b42c-7cd8478df768"), "San Jose Sharks", "Sharks", "[\"nhl\",\"hockey\"]" });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "Name", "ShortName", "Tags" },
                values: new object[] { new Guid("ff8968a2-0895-4b0c-a28d-fb3dff7d2b2d"), "Texas Stars", "Stars", "[\"ahl\",\"hockey\"]" });

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_Name",
                table: "Leagues",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_SiteId_Name",
                table: "Leagues",
                columns: new[] { "SiteId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueTeams_TeamId",
                table: "LeagueTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Name",
                table: "Sites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchers_GuildId_LeagueId_TeamId_Type_ChannelId",
                table: "Watchers",
                columns: new[] { "GuildId", "LeagueId", "TeamId", "Type", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchers_LeagueId",
                table: "Watchers",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchers_TeamId",
                table: "Watchers",
                column: "TeamId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildAdmins");

            migrationBuilder.DropTable(
                name: "LeagueTeams");

            migrationBuilder.DropTable(
                name: "Watchers");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
