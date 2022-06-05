using Discord.Commands;
using Discord.WebSocket;
using Duthie.Bot.Commands;
using Duthie.Bot.Configuration;
using Duthie.Bot.Extensions;
using Duthie.Services.Guilds;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Events;

public class DiscordEventHandler : IAsyncHandler
{
    private readonly ILogger<DiscordEventHandler> _logger;
    private readonly DiscordShardedClient _client;
    private readonly DiscordConfiguration _config;
    private readonly AppInfo _appInfo;

    private bool Connected { get; set; } = false;

    public DiscordEventHandler(
        ILogger<DiscordEventHandler> logger,
        DiscordShardedClient client,
        DiscordConfiguration config,
        AppInfo appInfo)
    {
        _logger = logger;
        _client = client;
        _config = config;
        _appInfo = appInfo;
    }

    public ValueTask RunAsync()
    {
        _client.ShardConnected += HandleConnectedAsync;
        _client.ShardReady += HandleReadyAsync;
        _client.ShardDisconnected += HandleDisconnectedAsync;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client.ShardDisconnected -= HandleDisconnectedAsync;
        _client.ShardReady -= HandleReadyAsync;
        _client.ShardConnected -= HandleConnectedAsync;
        return ValueTask.CompletedTask;
    }

    private async Task HandleConnectedAsync(DiscordSocketClient client)
    {
        if (!Connected)
        {
            _logger.LogInformation($"Connected to Discord! {_appInfo.Name} is active in {client.Guilds.Count()} guild{(client.Guilds.Count() != 1 ? "s" : "")}");
            await client.ShowStartingStatusAsync();
        }
        else
        {
            _logger.LogWarning($"Reconnected to Discord");
            await client.ShowOnlineStatusAsync();
        }

        Connected = true;
    }

    private async Task HandleReadyAsync(DiscordSocketClient client)
    {
        await client.ShowOnlineStatusAsync();
    }

    private async Task HandleDisconnectedAsync(Exception e, DiscordSocketClient client)
    {
        _logger.LogWarning($"Disconnected from Discord");
        await client.ShowDisconnectedStatusAsync();
    }
}