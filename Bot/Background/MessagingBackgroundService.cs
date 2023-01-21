using System.Diagnostics;
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
        _logger.LogTrace("Starting message sending task");
        var sw = Stopwatch.StartNew();

        try
        {
            var messages = (await _guildMessageService.GetUnsentAsync())
                .OrderBy(m => m.Timestamp ?? m.CreatedAt)
                    .ThenBy(m => m.GuildId);

            if (messages.Count() > 0)
                _logger.LogTrace($"Sending {MessageUtils.Pluralize(messages.Count(), "message")} to Discord");

            await Task.WhenAll(messages
                .GroupBy(m =>
                {
                    var guild = _client.GetGuild(m.GuildId);

                    return new
                    {
                        Guild = guild,
                        Channel = (guild?.GetChannel(m.ChannelId) ?? guild?.SystemChannel) as IMessageChannel
                    };
                })
                .Where(g => g.Key.Guild != null && g.Key.Channel != null)
                .Select(async messages =>
                {
                    var guild = messages.Key.Guild!;
                    var channel = messages.Key.Channel!;
                    await channel.TriggerTypingAsync();

                    foreach (var message in messages)
                    {
                        try
                        {
                            await channel.SendMessageAsync("", embed: new EmbedBuilder()
                                .WithColor((Color?)message.Color ?? Color.Default)
                                .WithTitle(message.Title)
                                .WithThumbnailUrl(message.Thumbnail)
                                .WithDescription(message.Content)
                                .WithFooter(message.Footer)
                                .WithUrl(message.Url)
                                .WithTimestamp(message.Timestamp ?? message.CreatedAt).Build());

                            message.ChannelId = channel.Id;
                            message.SentAt = DateTimeOffset.UtcNow;
                            await _guildMessageService.SaveAsync(message);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while sending message {message.Id}");
                        }
                    }
                }));

            sw.Stop();
            _logger.LogTrace($"Message sending task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Message sending task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during message sending task.");
        }
    }
}