using Duthie.Services.Background;
using Duthie.Services.Guilds;
using Duthie.Types.Guilds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Programs;

public class CommandLine
{
    private CommandLine() { }

    public static async Task RunAsync(params string[] args)
    {
        if (args.Length == 0)
            return;

        switch (args[0].ToLower().Trim())
        {
            case "broadcast":
                await BroadcastMessageAsync(args.Skip(1).ToArray());
                break;

            case "update":
                await UpdateDataAsync(args.Skip(1).ToArray());
                break;

            default:
                return;
        }

        Environment.Exit(0);
    }

    private static async Task BroadcastMessageAsync(params string[] args)
    {
        var message = string.Join(" ", args);

        var serviceProvider = new ServiceCollection()
            .ConfigureServices()
            .AddAppInfo()
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<CommandLine>>();
        var guildService = serviceProvider.GetRequiredService<GuildService>();
        var guildMessageService = serviceProvider.GetRequiredService<GuildMessageService>();

        foreach (var guild in await guildService.GetAllAsync())
        {
            logger.LogInformation($"Sending message to guild \"{guild.Name}\" [{guild.Id}]");

            await guildMessageService.SaveAsync(new GuildMessage
            {
                GuildId = guild.Id,
                ChannelId = 0,
                Message = message
            });
        }

        Environment.Exit(0);
    }

    private static async Task UpdateDataAsync(params string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .ConfigureServices()
            .AddApi()
            .AddAppInfo()
            .BuildServiceProvider();

        await Task.WhenAll(args.Select(async type =>
        {
            switch (type.Trim().ToLower())
            {
                case "all":
                    await serviceProvider.GetRequiredService<LeagueUpdateService>().UpdateAll();
                    await serviceProvider.GetRequiredService<GameUpdateService>().UpdateGames();
                    break;

                case "games":
                    await serviceProvider.GetRequiredService<GameUpdateService>().UpdateGames();
                    break;

                case "leagues":
                    await serviceProvider.GetRequiredService<LeagueUpdateService>().UpdateAll();
                    break;

                case "league-info":
                    await serviceProvider.GetRequiredService<LeagueUpdateService>().UpdateInfo();
                    break;

                case "teams":
                    await serviceProvider.GetRequiredService<LeagueUpdateService>().UpdateTeams();
                    break;
            }
        }));
    }
}