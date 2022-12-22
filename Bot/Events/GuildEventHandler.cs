using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Duthie.Bot.Extensions;
using Duthie.Services.Guilds;
using Duthie.Types.Guilds;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Events;

public class GuildEventHandler : IAsyncHandler
{
    private readonly ILogger<GuildEventHandler> _logger;
    private readonly DiscordShardedClient _client;
    private readonly DiscordConfiguration _config;
    private readonly GuildService _guildService;
    private readonly GuildAdminService _guildAdminService;
    private readonly AppInfo _appInfo;

    public GuildEventHandler(
        ILogger<GuildEventHandler> logger,
        DiscordShardedClient client,
        DiscordConfiguration config,
        GuildService guildService,
        GuildAdminService guildAdminService,
        AppInfo appInfo)
    {
        _logger = logger;
        _client = client;
        _config = config;
        _guildService = guildService;
        _guildAdminService = guildAdminService;
        _appInfo = appInfo;
    }

    public ValueTask RunAsync()
    {
        _client.ShardReady += HandleReadyAsync;

        _client.JoinedGuild += HandleJoinedGuildAsync;
        _client.LeftGuild += HandleLeftGuildAsync;
        _client.GuildUpdated += HandleGuildUpdatedAsync;
        _client.UserLeft += HandleUserLeftAsync;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client.UserLeft -= HandleUserLeftAsync;
        _client.GuildUpdated -= HandleGuildUpdatedAsync;
        _client.LeftGuild -= HandleLeftGuildAsync;
        _client.JoinedGuild -= HandleJoinedGuildAsync;
        _client.ShardReady -= HandleReadyAsync;
        return ValueTask.CompletedTask;
    }

    private async Task HandleReadyAsync(DiscordSocketClient client)
    {
        _client.ShardReady -= HandleReadyAsync;

        _logger.LogDebug("Updating guilds");
        var sw = Stopwatch.StartNew();

        try
        {
            await UpdateGuildsAsync(client);

            sw.Stop();
            _logger.LogTrace($"Guild update completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Guild update failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during guild update.");
            Environment.Exit(0);
        }
    }

    private async Task HandleJoinedGuildAsync(SocketGuild guild)
    {
        await JoinAsync(guild.ToGuild());
    }

    private async Task HandleGuildUpdatedAsync(SocketGuild oldGuild, SocketGuild newGuild)
    {
        if (oldGuild.Name != newGuild.Name)
            await RenameAsync(newGuild.Id, newGuild.Name, oldGuild.Name);
    }

    private async Task HandleLeftGuildAsync(SocketGuild guild)
    {
        await LeaveAsync(guild.ToGuild());
    }

    private async Task HandleUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if ((await _guildAdminService.DeleteAsync(guild.Id, user.Id)) > 0)
            _logger.LogDebug($"{_appInfo.Name} administrator {user} has left guild \"{guild.Name}\" [{guild.Id}]");
    }

    private async Task JoinAsync(Guild guild)
    {
        _logger.LogDebug($"{_appInfo.Name} has joined guild \"{guild.Name}\" [{guild.Id}]");
        guild.LeftAt = null;
        await _guildService.SaveAsync(guild);

        var socketGuild = _client.Guilds.FirstOrDefault(g => g.Id == guild.Id);
        if (socketGuild == null) return;

        var admins = await _guildAdminService.GetAllAsync(guild.Id);

        foreach (var memberId in admins)
        {
            if (!socketGuild.Users.Any(u => u.Id == memberId))
            {
                var user = _client.GetUser(memberId);

                _logger.LogDebug($"{_appInfo.Name} administrator {user} is no longer a member of guild \"{guild.Name}\" [{guild.Id}]");
                await _guildAdminService.DeleteAsync(guild.Id, memberId);
            }
        }
    }

    private async Task LeaveAsync(Guild guild)
    {
        _logger.LogDebug($"{_appInfo.Name} has left guild \"{guild.Name}\" [{guild.Id}]");
        await _guildService.DeleteAsync(guild.Id);
    }

    private async Task RenameAsync(ulong id, string newName, string? oldName = null)
    {
        if (!string.IsNullOrWhiteSpace(oldName))
            _logger.LogDebug($"Renaming guild \"{oldName}\" to \"{newName}\" [{id}]");
        else
            _logger.LogDebug($"Renaming guild \"{newName}\" [{id}]");

        var guild = await _guildService.GetAsync(id);

        if (guild != null)
        {
            guild.Name = newName;
            await _guildService.SaveAsync(guild);
        }
    }

    private async Task UpdateGuildsAsync(DiscordSocketClient client)
    {
        var guilds = await _guildService.GetAllAsync();
        var activeGuilds = client.Guilds.ToDictionary(guild => guild.Id, guild => guild);

        foreach (var guild in guilds)
        {
            if (guild.LeftAt == null && !activeGuilds.ContainsKey(guild.Id))
                await LeaveAsync(guild);
            else if (guild.LeftAt != null && activeGuilds.ContainsKey(guild.Id))
                await JoinAsync(guild);
            else if (activeGuilds.ContainsKey(guild.Id))
            {
                guild.Name = activeGuilds[guild.Id].Name;
                guild.DefaultChannelId = activeGuilds[guild.Id].DefaultChannel.Id;
                await _guildService.SaveAsync(guild);
            }
        }

        var guildIds = guilds.Select(guild => guild.Id);

        foreach (var guild in client.Guilds)
        {
            if (guildIds.Contains(guild.Id))
            {
                var adminIds = await _guildAdminService.GetAllAsync(guild.Id);
                var invalidAdminIds = adminIds.Where(adminId => guild.GetUser(adminId) == null);

                if (invalidAdminIds.Count() > 0)
                    await _guildAdminService.DeleteAsync(guild.Id, invalidAdminIds.ToArray());
            }
            else
                await JoinAsync(guild.ToGuild());
        }

        await _guildService.PruneAsync();
    }
}