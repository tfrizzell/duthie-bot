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
                .OrderBy(m => m.CreatedAt)
                    .ThenBy(m => m.Guild.JoinedAt);

            if (messages.Count() > 0)
                _logger.LogTrace($"Sending {MessageUtils.Pluralize(messages.Count(), "message")} to Discord");

            await Task.WhenAll(messages.Select(async message =>
            {
                var guild = _client.GetGuild(message.GuildId);

                if (guild == null)
                    return;

                var channel = (guild.GetChannel(message.ChannelId) ?? guild.DefaultChannel) as IMessageChannel;

                if (channel == null)
                    return;

                await channel.TriggerTypingAsync();
                Embed? embed = null;

                if (message.Embed != null)
                {
                    var builder = new EmbedBuilder()
                        .WithColor((Color?)message.Embed.Color ?? Color.Default)
                        .WithTitle(message.Embed.Title)
                        .WithThumbnailUrl(message.Embed.Thumbnail)
                        .WithDescription(message.Embed.Content)
                        .WithFooter(message.Embed.Footer)
                        .WithUrl(message.Embed.Url);

                    if (message.Embed.ShowAuthor)
                        builder.WithAuthor(_client.CurrentUser);

                    if (message.Embed.Timestamp != null)
                        builder.WithTimestamp(message.Embed.Timestamp.GetValueOrDefault());
                    else
                        builder.WithCurrentTimestamp();

                    embed = builder.Build();
                }

                await channel.SendMessageAsync(message.Message, embed: embed);

                message.ChannelId = channel.Id;
                message.SentAt = DateTimeOffset.UtcNow;
                await _guildMessageService.SaveAsync(message);
            }));

            sw.Stop();
            _logger.LogTrace($"Message sending task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Message sending task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during message sending task.");
        }
    }
}