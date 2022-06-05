using Discord;
using Discord.WebSocket;
using Duthie.Bot.Utils;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Sites;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class MessagingBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<MessagingBackgroundService> _logger;
    private readonly DiscordShardedClient _client;
    private readonly GuildMessageService _guildMessageService;
    private readonly SiteService _siteService;
    private readonly LeagueService _leagueService;

    public MessagingBackgroundService(
        ILogger<MessagingBackgroundService> logger,
        DiscordShardedClient client,
        GuildMessageService guildMessageService,
        SiteService siteService,
        LeagueService leagueService) : base(logger)
    {
        _logger = logger;
        _client = client;
        _guildMessageService = guildMessageService;
        _siteService = siteService;
        _leagueService = leagueService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "*/30 * * * * *"
        };
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        try
        {
            _logger.LogTrace("Checking for unsent messages");
            var messages = await _guildMessageService.GetUnsentAsync();

            if (messages.Count() > 0)
                _logger.LogDebug($"Sending {MessageUtils.Pluralize(messages.Count(), "message")} to Discord");

            await Task.WhenAll(messages.Select(async message =>
            {
                var guild = _client.GetGuild(message.GuildId);

                if (guild == null)
                    return;

                var channel = (guild.GetChannel(message.ChannelId) ?? guild.DefaultChannel) as IMessageChannel;

                if (channel == null)
                    return;

                await channel.TriggerTypingAsync();
                await channel.SendMessageAsync(message.Message);

                message.ChannelId = channel.Id;
                message.SentAt = DateTimeOffset.UtcNow;
                await _guildMessageService.SaveAsync(message);
            }));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while sending guild messages.");
        }
    }
}