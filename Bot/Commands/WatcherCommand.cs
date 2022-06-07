using Discord;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Duthie.Bot.Extensions;
using Duthie.Bot.Utils;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Sites;
using Duthie.Services.Teams;
using Duthie.Services.Watchers;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class WatcherCommand : BaseCommandWithAdminCheck
{
    private static readonly Guid TYPE_ALL = new Guid("31625d2a-6587-477f-a888-84968d5b5eff");
    private static readonly Guid TYPE_ALL_NEWS = new Guid("1f20dd2c-df09-46ba-9d57-5ab67d8a910a");

    private readonly ILogger<WatcherCommand> _logger;
    private readonly AppInfo _appInfo;
    private readonly WatcherService _watcherService;
    private readonly LeagueService _leagueService;
    private readonly TeamService _teamService;
    private readonly SiteService _siteService;

    public WatcherCommand(
        ILogger<WatcherCommand> logger,
        AppInfo appInfo,
        WatcherService watcherService,
        LeagueService leagueService,
        TeamService teamService,
        SiteService siteService,
        DiscordConfiguration config,
        GuildAdminService guildAdminService) : base(config, guildAdminService)
    {
        _logger = logger;
        _appInfo = appInfo;
        _watcherService = watcherService;
        _leagueService = leagueService;
        _teamService = teamService;
        _siteService = siteService;
    }

    protected override string Command { get => "watcher"; }

    public override async Task<SlashCommandOptionBuilder> BuildAsync() =>
        new SlashCommandOptionBuilder()
            .WithName(Command)
            .WithDescription($"Add, remove, or view {_appInfo.Name} watchers for your server.")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(await BuildAddAsync())
            .AddOption(await BuildListAsync())
            .AddOption(await BuildRemoveAsync());

    private async Task<SlashCommandOptionBuilder> BuildAddAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription($"Add a new {_appInfo.Name} watcher for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddLeagueOption(cmd, true);
        await AddTeamOption(cmd, true);
        await AddWatcherTypeOption(cmd, true);
        await AddChannelOption(cmd);
        return cmd;
    }

    public async Task<SlashCommandOptionBuilder> BuildListAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("list")
            .WithDescription($"View the {_appInfo.Name} watchers for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddSiteFilter(cmd);
        await AddLeagueFilter(cmd);
        await AddTeamFilter(cmd);
        await AddWatcherTypeFilter(cmd);
        await AddChannelFilter(cmd);
        return cmd;
    }

    private async Task<SlashCommandOptionBuilder> BuildRemoveAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("remove")
            .WithDescription($"Remove a {_appInfo.Name} watcher for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("all", ApplicationCommandOptionType.Boolean, "removes all watchers");

        await AddLeagueOption(cmd, false);
        await AddTeamOption(cmd, false);
        await AddWatcherTypeOption(cmd, false);
        await AddChannelOption(cmd);
        return cmd;
    }

    private Task AddChannelOption(SlashCommandOptionBuilder cmd)
    {
        var channelOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.Channel)
            .WithName("channel")
            .WithDescription("the channel to send messages to");

        cmd.AddOption(channelOption);
        return Task.CompletedTask;
    }

    private Task AddLeagueOption(SlashCommandOptionBuilder cmd, bool required)
    {
        var leagueOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("league")
            .WithDescription("the leagues to watch")
            .WithRequired(required);

        cmd.AddOption(leagueOption);
        return Task.CompletedTask;
    }

    private Task AddTeamOption(SlashCommandOptionBuilder cmd, bool required)
    {
        var teamOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("team")
            .WithDescription("the teams to watch")
            .WithRequired(required);

        cmd.AddOption(teamOption);
        return Task.CompletedTask;
    }

    private Task AddWatcherTypeOption(SlashCommandOptionBuilder cmd, bool required)
    {
        var watcherTypes = Enum.GetValues<WatcherType>();

        if (watcherTypes.Count() == 0)
            return Task.CompletedTask;

        var watcherTypeOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("type")
            .WithDescription("the type of watchers to register")
            .WithRequired(required);

        watcherTypeOption.AddChoice("All", TYPE_ALL.ToString());
        watcherTypeOption.AddChoice("All News", TYPE_ALL_NEWS.ToString());

        foreach (var watcherType in watcherTypes)
            watcherTypeOption.AddChoice(EnumUtils.GetName(watcherType), watcherType.ToString());

        cmd.AddOption(watcherTypeOption);
        return Task.CompletedTask;
    }

    protected override async Task HandleCommandAsync(SocketSlashCommand command)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await IsOwnerAsync(user) && !await IsAdministratorAsync(user))
        {
            _logger.LogWarning($"Admin command issued in guild \"{guild.Name}\" [{guild.Id}] by non-administrator {command.User}");
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but you don't have permission to do that!", ephemeral: true);
            return;
        }

        try
        {
            var cmd = command.Data.Options.First().Options.First();

            switch (cmd.Name)
            {
                case "add":
                    await AddWatchersAsync(command, cmd);
                    break;

                case "list":
                    await ListWatchersAsync(command, cmd);
                    break;

                case "remove":
                    await RemoveWatchersAsync(command, cmd);
                    break;

                default:
                    await SendUnrecognizedAsync(command);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error has occurred while handling watcher command.");
            await SendErrorAsync(command);
        }
    }

    private async Task AddWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await CheckPrivileges(command))
            return;

        var (league, leagueOption) = await GetLeagueAsync(cmd);
        var (team, teamOption) = await GetTeamAsync(cmd);
        var (watcherTypes, watcherTypeOption) = await GetWatcherTypeAsync(cmd);
        var channel = await GetChannelAsync(cmd);

        if (league == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        if (team == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }
        else if (!league.LeagueTeams.Any(lt => lt.TeamId == team.Id))
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{team.Name}` in the league {league.Name}.", ephemeral: true);
            return;
        }

        if (watcherTypes == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{watcherTypeOption}`.", ephemeral: true);
            return;
        }

        var watchers = watcherTypes.Select(watcherType =>
            new Watcher
            {
                GuildId = guild.Id,
                LeagueId = league.Id,
                TeamId = team.Id,
                Type = watcherType,
                ChannelId = channel?.Id
            });

        var count = await _watcherService.SaveAsync(watchers.ToArray());

        if (count > 0)
        {
            _logger.LogDebug($"User {user} added {MessageUtils.Pluralize(count, "watcher")} to guild \"{guild.Name}\" [{guild.Id}]");
            await command.RespondAsync($"Okay! I've added {MessageUtils.Pluralize(count, "watcher")} to your server.", ephemeral: true);
        }
        else
            await command.RespondAsync($"Okay! There were no new watchers to add.", ephemeral: true);
    }

    public async Task ListWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        var (league, leagueOption) = await GetLeagueAsync(cmd, true);
        var (team, teamOption) = await GetTeamAsync(cmd, true);
        var (watcherTypes, watcherTypeOption) = await GetWatcherTypeAsync(cmd);
        var channel = await GetChannelAsync(cmd);

        if (leagueOption != null && league == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        if (teamOption != null && team == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }

        if (watcherTypeOption != null && watcherTypes == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{watcherTypeOption}`.", ephemeral: true);
            return;
        }

        var watchers = await _watcherService.FindAsync(guild.Id,
            leagues: league == null ? null : new Guid[] { league.Id },
            teams: team == null ? null : new Guid[] { team.Id },
            types: watcherTypes,
            channels: channel == null ? null : new ulong?[] { channel.Id });

        if (watchers.Count() > 0)
        {
            await command.RespondAsync(ListUtils.CreateTable(
                headers: new string[] {
                    "League",
                    "Team",
                    "Type",
                    "Channel",
                    "Site"
                },
                data: watchers.Select(w => new string[] {
                    w.League.Name,
                    w.Team.Name,
                    EnumUtils.GetName(w.Type),
                    (guild.Channels.FirstOrDefault(c => c.Id == w.ChannelId) ?? guild.DefaultChannel).Name,
                    w.League.Site.Name
                })), ephemeral: true);

            _logger.LogTrace($"User {user} viewed watcher list for guild \"{guild.Name}\" [{guild.Id}]");
        }
        else
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I didn't find any watchers that matched your request.", ephemeral: true);
    }

    private async Task RemoveWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await CheckPrivileges(command))
            return;

        var watchers = new List<Watcher>();
        bool removeAll = false;
        bool.TryParse(cmd.Options.FirstOrDefault(o => "all" == o.Name)?.Value?.ToString() ?? "", out removeAll);

        if (!removeAll)
        {
            var (league, leagueOption) = await GetLeagueAsync(cmd);
            var (team, teamOption) = await GetTeamAsync(cmd);
            var (watcherTypes, watcherTypeOption) = await GetWatcherTypeAsync(cmd);
            var channel = await GetChannelAsync(cmd);

            if (leagueOption != null && league == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
                return;
            }

            if (teamOption != null && team == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the teams `{teamOption}`.", ephemeral: true);
                return;
            }

            if (watcherTypeOption != null && watcherTypes == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher types `{watcherTypeOption}`.", ephemeral: true);
                return;
            }

            watchers.AddRange(await _watcherService.FindAsync(guild.Id,
                leagues: league == null ? null : new Guid[] { league.Id },
                teams: team == null ? null : new Guid[] { team.Id },
                types: watcherTypes,
                channels: channel == null ? null : new List<ulong?>() { channel.Id }));
        }
        else
            watchers.AddRange(await _watcherService.GetAllAsync(guild.Id));

        var count = await _watcherService.DeleteAsync(watchers.Select(w => w.Id).ToArray());

        if (count > 0)
        {
            _logger.LogDebug($"User {user} deleted {MessageUtils.Pluralize(count, "watcher")} from guild \"{guild.Name}\" [{guild.Id}]");
            await command.RespondAsync($"Okay! I've deleted {MessageUtils.Pluralize(count, "watcher")} to your server.", ephemeral: true);
        }
        else
            await command.RespondAsync($"Okay! There were no watchers to delete.", ephemeral: true);
    }

    private async Task<bool> CheckPrivileges(SocketSlashCommand command)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await IsOwnerAsync(user) && !await IsAdministratorAsync(user))
        {
            _logger.LogWarning($"Watcher command issued in guild \"{guild.Name}\" [{guild.Id}] by non-administrator {command.User}");
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but you don't have permission to do that!", ephemeral: true);
            return false;
        }

        return true;
    }

    private Task<SocketTextChannel?> GetChannelAsync(SocketSlashCommandDataOption cmd) =>
        Task.FromResult((SocketTextChannel?)cmd.Options.FirstOrDefault(o => "channel" == o.Name)?.Value);

    private async Task<(League?, string?)> GetLeagueAsync(SocketSlashCommandDataOption cmd, bool returnNullForAll = false)
    {
        var leagueOption = cmd.Options.FirstOrDefault(o => "league" == o.Name)?.Value?.ToString();
        if (leagueOption == null) return (null, leagueOption);

        var league = (await _leagueService.FindAsync(leagueOption)).FirstOrDefault();
        return (league, leagueOption);
    }

    private async Task<(Team?, string?)> GetTeamAsync(SocketSlashCommandDataOption cmd, bool returnNullForAll = false)
    {
        var teamOption = cmd.Options.FirstOrDefault(o => "team" == o.Name)?.Value?.ToString();
        if (teamOption == null) return (null, teamOption);

        var team = (await _teamService.FindAsync(teamOption)).FirstOrDefault();
        return (team, teamOption);
    }

    private Task<(IEnumerable<WatcherType>?, string?)> GetWatcherTypeAsync(SocketSlashCommandDataOption cmd)
    {
        var watcherType = cmd.Options.FirstOrDefault(o => "type" == o.Name)?.Value?.ToString();
        if (watcherType == null) return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((null, watcherType));

        var watcherTypes = new List<WatcherType>();

        if (Enum.TryParse<WatcherType>(watcherType, out var watcherTypeEnum))
            watcherTypes.Add(watcherTypeEnum);
        else if (watcherType == TYPE_ALL_NEWS.ToString())
            watcherTypes.AddRange(Enum.GetValues<WatcherType>().Where(type => type != WatcherType.Games));
        else if (watcherType == TYPE_ALL.ToString())
            watcherTypes.AddRange(Enum.GetValues<WatcherType>());
        else
            watcherTypes = null;

        return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((watcherTypes, watcherType));
    }
}