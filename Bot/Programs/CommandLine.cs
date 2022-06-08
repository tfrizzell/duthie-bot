using Duthie.Bot.Background;
using Duthie.Data;
using Duthie.Services.Guilds;
using Duthie.Types.Guilds;
using Microsoft.EntityFrameworkCore;
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

            case "prune":
                await PruneDataAsync(args.Skip(1).ToArray());
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
        var serviceProvider = new ServiceCollection()
            .ConfigureServices()
            .AddAppInfo()
            .BuildServiceProvider();

        await UpdateDatabaseAsync(serviceProvider);

        var message = string.Join(" ", args);
        var logger = serviceProvider.GetRequiredService<ILogger<CommandLine>>();
        var guildService = serviceProvider.GetRequiredService<GuildService>();
        var guildMessageService = serviceProvider.GetRequiredService<GuildMessageService>();

        foreach (var guild in await guildService.GetAllAsync())
        {
            await guildMessageService.SaveAsync(new GuildMessage
            {
                GuildId = guild.Id,
                ChannelId = 0,
                Message = message
            });

            logger.LogTrace($"Sent message to guild \"{guild.Name}\" [{guild.Id}]");
        }

        Environment.Exit(0);
    }

    private static async Task PruneDataAsync(params string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .ConfigureServices()
            .AddApi()
            .AddAppInfo()
            .AddSingleton<PruningBackgroundService>()
            .BuildServiceProvider();

        await UpdateDatabaseAsync(serviceProvider);
        await serviceProvider.GetRequiredService<PruningBackgroundService>().ExecuteAsync();
    }

    private static async Task UpdateDataAsync(params string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .ConfigureServices()
            .AddApi()
            .AddAppInfo()
            .AddSingleton<LeagueBackgroundService>()
            .AddSingleton<TeamBackgroundService>()
            .AddSingleton<GameBackgroundService>()
            .AddSingleton<BidBackgroundService>()
            .AddSingleton<ContractBackgroundService>()
            .AddSingleton<DraftPickBackgroundService>()
            .AddSingleton<TradeBackgroundService>()
            .BuildServiceProvider();

        await UpdateDatabaseAsync(serviceProvider);

        var types = args.Select(arg => arg.Trim().ToLower());

        if (types.Intersect(new string[] { "leagues", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<LeagueBackgroundService>().ExecuteAsync();

        if (types.Intersect(new string[] { "teams", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<TeamBackgroundService>().ExecuteAsync();

        if (types.Intersect(new string[] { "games", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<GameBackgroundService>().ExecuteAsync();

        if (types.Intersect(new string[] { "bids", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<BidBackgroundService>().ExecuteAsync();

        if (types.Intersect(new string[] { "contracts", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<ContractBackgroundService>().ExecuteAsync();

        if (types.Intersect(new string[] { "draft", "draft-picks", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<DraftPickBackgroundService>().ExecuteAsync();

        if (types.Intersect(new string[] { "trades", "all" }).Count() > 0)
            await serviceProvider.GetRequiredService<TradeBackgroundService>().ExecuteAsync();
    }

    private static async Task UpdateDatabaseAsync(IServiceProvider serviceProvider)
    {
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<DuthieDbContext>>();

        using (var context = await contextFactory.CreateDbContextAsync())
        {
            context.Database.Migrate();
            await context.PopulateAsync();
        }
    }
}