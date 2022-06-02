// https://discordapp.com/oauth2/authorize?&client_id=435582099714605057&scope=bot&permissions=2048

using System.Reflection;
using Discord;
using Discord.WebSocket;
using Duthie.Bot;
using Duthie.Bot.Background;
using Duthie.Bot.Configuration;
using Duthie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await LoadModules();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services
            .ConfigureServices()
            .AddDiscord()
            .AddApi()
            .AddAppInfo();

        services
            .AddHostedService<Main>()
            .AddHostedService<LeagueUpdateBackgroundService>();
    })
    .Build();

await host.RunAsync();

Task LoadModules()
{
    var modules = new List<string> { };
    var moduleDir = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".", "modules");

    if (Directory.Exists(moduleDir))
        modules.AddRange(Directory.EnumerateFiles(moduleDir, "*.dll"));

    foreach (var module in modules)
        Assembly.LoadFile(module);

    return Task.CompletedTask;
}

internal class Main : BackgroundService
{
    private readonly ILogger<Main> _logger;
    private readonly DiscordConfiguration _config;
    private readonly IEnumerable<IAsyncHandler> _handlers;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly DiscordShardedClient _client;

    public Main(
        ILogger<Main> logger,
        DiscordConfiguration config,
        IEnumerable<IAsyncHandler> handlers,
        IDbContextFactory<DuthieDbContext> contextFactory,
        DiscordShardedClient client)
    {
        _logger = logger;
        _config = config;
        _handlers = handlers;
        _contextFactory = contextFactory;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_config.Token))
        {
            throw new ArgumentException($"Configuration option Discord:Token cannot be empty");
        }

        foreach (var handler in _handlers)
            await handler.RunAsync();

        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.Database.Migrate();
            await context.PopulateAsync();
        }

        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);

        await _client.StopAsync();
    }
}