using Discord;
using Discord.WebSocket;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Sites;
using Duthie.Types;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class MessagingBackgroundService : IHostedService, IDisposable
{
    private readonly ILogger<MessagingBackgroundService> _logger;
    private readonly DiscordShardedClient _client;
    private readonly GuildMessageService _guildMessageService;
    private readonly SiteService _siteService;
    private readonly LeagueService _leagueService;
    private Timer? _timer;

    public MessagingBackgroundService(
        ILogger<MessagingBackgroundService> logger,
        DiscordShardedClient client,
        GuildMessageService guildMessageService,
        SiteService siteService,
        LeagueService leagueService)
    {
        _logger = logger;
        _client = client;
        _guildMessageService = guildMessageService;
        _siteService = siteService;
        _leagueService = leagueService;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug($"Starting {GetType().Name}");
        _client.ShardReady += HandleReadyAsync;
        return Task.CompletedTask;
    }

    private Task HandleReadyAsync(DiscordSocketClient client)
    {
        _client.ShardReady -= HandleReadyAsync;
        _timer = new Timer(SendUnsentMessages, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(15));
        return Task.CompletedTask;
    }

    private async void SendUnsentMessages(object? state = null)
    {
        try
        {
            _logger.LogDebug("Checking for unsent messages");
            var messages = await _guildMessageService.GetUnsentAsync();

            if (messages.Count() > 1)
                _logger.LogInformation($"Sending {messages.Count()} messages to Discord");
            else if (messages.Count() == 1)
                _logger.LogInformation($"Sending {messages.Count()} message to Discord");

            foreach (var message in messages)
            {
                var guild = _client.GetGuild(message.GuildId);

                if (guild == null)
                    continue;

                var channel = (guild.GetChannel(message.ChannelId) ?? guild.DefaultChannel) as IMessageChannel;

                if (channel == null)
                    continue;

                await channel.TriggerTypingAsync();
                await channel.SendMessageAsync(message.Message);

                message.ChannelId = channel.Id;
                message.SentAt = DateTimeOffset.UtcNow;
                await _guildMessageService.SaveAsync(message);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while sending guild messages.");
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug($"Stopping {GetType().Name}");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.ShardReady -= HandleReadyAsync;
        _timer?.Dispose();
    }
}