using System.Diagnostics;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Duthie.Bot.Extensions;
using Duthie.Services.Guilds;
using Duthie.Types;
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

        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Updating guilds");

        try
        {
            await UpdateGuildsAsync(client);
            sw.Stop();

            _logger.LogDebug($"Guild update completed in {sw.Elapsed.TotalMilliseconds} milliseconds");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogError(e, $"Guild update failed in {sw.Elapsed.TotalMilliseconds} milliseconds");
            Environment.Exit(0);
        }
    }

    private async Task HandleJoinedGuildAsync(SocketGuild guild)
    {
        await JoinAsync(guild.ToGuild());
    }

    private async Task HandleGuildUpdatedAsync(SocketGuild oldGuild, SocketGuild newGuild)
    {
        if (!oldGuild.Name.Equals(newGuild.Name))
            await RenameAsync(newGuild.Id, newGuild.Name, oldGuild.Name);
    }

    private async Task HandleLeftGuildAsync(SocketGuild guild)
    {
        await LeaveAsync(guild.ToGuild());
    }

    private async Task HandleUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (await _guildAdminService.ExistsAsync(guild.Id, user.Id))
        {
            _logger.LogInformation($"{_appInfo.Name} administrator {user} has left guild \"{guild.Name}\" [{guild.Id}]");
            await _guildAdminService.DeleteAsync(guild.Id, user.Id);
        }
    }

    private async Task JoinAsync(Guild guild)
    {
        _logger.LogInformation($"{_appInfo.Name} has joined guild \"{guild.Name}\" [{guild.Id}]");
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

                _logger.LogInformation($"{_appInfo.Name} administrator {user} is no longer a member of guild \"{guild.Name}\" [{guild.Id}]");
                await _guildAdminService.DeleteAsync(guild.Id, memberId);
            }
        }
    }

    private async Task LeaveAsync(Guild guild)
    {
        _logger.LogInformation($"{_appInfo.Name} has left guild \"{guild.Name}\" [{guild.Id}]");
        await _guildService.DeleteAsync(guild.Id);
    }

    private async Task RenameAsync(ulong id, string newName, string? oldName = null)
    {
        if (!string.IsNullOrWhiteSpace(oldName))
            _logger.LogDebug($"Renaming guild \"{oldName}\" to \"{newName}\" [{id}]");
        else
            _logger.LogDebug($"Renaming guild \"{newName}\" [{id}]");

        await _guildService.RenameAsync(id, newName);
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
            else if (activeGuilds.ContainsKey(guild.Id) && !guild.Name.Equals(activeGuilds[guild.Id].Name))
                await RenameAsync(guild.Id, activeGuilds[guild.Id].Name, guild.Name);
        }

        var guildIds = guilds.Select(guild => guild.Id);

        foreach (var guild in client.Guilds)
        {
            if (!guildIds.Contains(guild.Id))
                await JoinAsync(guild.ToGuild());
        }

        await _guildService.PruneAsync();
    }
}