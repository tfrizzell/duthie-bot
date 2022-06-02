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
using Duthie.Types;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class WatchersCommand : BaseCommandWithAdminCheck
{
    private static readonly Guid TYPE_ALL = new Guid("31625d2a-6587-477f-a888-84968d5b5eff");
    private static readonly Guid TYPE_ALL_NEWS = new Guid("1f20dd2c-df09-46ba-9d57-5ab67d8a910a");

    private readonly ILogger<WatchersCommand> _logger;
    private readonly AppInfo _appInfo;
    private readonly WatcherService _watcherService;
    private readonly LeagueService _leagueService;
    private readonly TeamService _teamService;
    private readonly SiteService _siteService;

    public WatchersCommand(
        ILogger<WatchersCommand> logger,
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
            _logger.LogError(e, "An unexpected error has occurred");
            await SendErrorAsync(command);
        }
    }

    private async Task AddWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await CheckPrivileges(command))
            return;

        var (leagues, leagueOption) = await GetLeaguesAsync(cmd);
        var (teams, teamOption) = await GetTeamAsync(cmd);
        var (watcherTypes, watcherTypeOption) = await GetWatcherTypeAsync(cmd);
        var channel = await GetChannelAsync(cmd);

        if (leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league(s) you requested.", ephemeral: true);
            return;
        }

        if (teams == null || !teams.Any(t => leagues.Any(l => l.Teams.Any(lt => lt.Id == t.Id))))
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team(s) you requested.", ephemeral: true);
            return;
        }

        if (watcherTypes == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type(s) you requested.", ephemeral: true);
            return;
        }

        var watchers = new List<Watcher>();

        foreach (var league in leagues)
        {
            foreach (var team in teams)
            {
                if (!league.Teams.Any(t => t.Id == team.Id))
                    continue;

                foreach (var watcherType in watcherTypes)
                {
                    watchers.Add(new Watcher
                    {
                        GuildId = guild.Id,
                        LeagueId = league.Id,
                        TeamId = team.Id,
                        Type = watcherType,
                        ChannelId = channel?.Id
                    });
                }
            }
        }

        var count = await _watcherService.SaveAsync(watchers.ToArray());

        if (count > 0)
        {
            _logger.LogDebug($"User {user} added {count} new watchers to guild guild \"{guild.Name}\" [{guild.Id}]");
            await command.RespondAsync($"Okay! I've added {count} new watchers to your server.", ephemeral: true);
        }
        else
            await command.RespondAsync($"All of the watchers you requested already exist.", ephemeral: true);
    }

    public async Task ListWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        _logger.LogDebug($"User {user} viewed watcher list for guild \"{guild.Name}\" [{guild.Id}]");

        var (leagues, leagueOption) = await GetLeaguesAsync(cmd);
        var (teams, teamOption) = await GetTeamAsync(cmd);
        var (watcherTypes, watcherTypeOption) = await GetWatcherTypeAsync(cmd);
        var channel = await GetChannelAsync(cmd);

        if (leagueOption != null && leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league you requested.", ephemeral: true);
            return;
        }

        if (teamOption != null && teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team you requested.", ephemeral: true);
            return;
        }

        if (watcherTypeOption != null && watcherTypes == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type you requested.", ephemeral: true);
            return;
        }

        var watchers = await _watcherService.FindAsync(guild.Id,
            leagues: leagues?.Select(l => l.Id),
            teams: teams?.Select(t => t.Id),
            types: watcherTypes,
            channels: channel == null ? null : new List<ulong?>() { channel.Id });

        if (watchers.Count() > 0)
        {
            await command.RespondAsync(ListUtils.DrawBox(
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
        }
        else
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find any watchers on your server.", ephemeral: true);
    }

    private async Task RemoveWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await CheckPrivileges(command))
            return;

        var watchers = new List<Watcher>();
        bool removeAll = false;
        bool.TryParse(cmd.Options.FirstOrDefault(o => "all".Equals(o.Name))?.Value?.ToString() ?? "", out removeAll);

        if (!removeAll)
        {
            var (leagues, leagueOption) = await GetLeaguesAsync(cmd);
            var (teams, teamOption) = await GetTeamAsync(cmd);
            var (watcherTypes, watcherTypeOption) = await GetWatcherTypeAsync(cmd);
            var channel = await GetChannelAsync(cmd);

            if (leagueOption != null && leagues == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league(s) you requested.", ephemeral: true);
                return;
            }

            if (teamOption != null && (teams == null || !teams.Any(t => leagues?.Any(l => l.Teams.Any(lt => lt.Id == t.Id)) != false)))
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team(s) you requested.", ephemeral: true);
                return;
            }

            if (watcherTypeOption != null && watcherTypes == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type(s) you requested.", ephemeral: true);
                return;
            }

            watchers.AddRange(await _watcherService.FindAsync(guild.Id,
                leagues: leagues?.Select(l => l.Id),
                teams: teams?.Select(t => t.Id),
                types: watcherTypes,
                channels: channel == null ? null : new List<ulong?>() { channel.Id }));
        }
        else
            watchers.AddRange(await _watcherService.GetAllAsync(guild.Id));

        var count = await _watcherService.DeleteAsync(watchers.Select(w => w.Id).ToArray());

        if (count > 0)
        {
            _logger.LogDebug($"User {user} deleted {count} watchers to guild guild \"{guild.Name}\" [{guild.Id}]");
            await command.RespondAsync($"Okay! I've deleted {count} watchers to your server.", ephemeral: true);
        }
        else
            await command.RespondAsync($"None of the watchers you requested exist.", ephemeral: true);
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
        Task.FromResult((SocketTextChannel?)cmd.Options.FirstOrDefault(o => "channel".Equals(o.Name))?.Value);

    private async Task<(IEnumerable<League>?, string?)> GetLeaguesAsync(SocketSlashCommandDataOption cmd)
    {
        var leagueOption = cmd.Options.FirstOrDefault(o => "league".Equals(o.Name))?.Value?.ToString();
        if (leagueOption == null) return (null, leagueOption);

        var leagues = new List<League>();

        if (!leagueOption.Equals(TYPE_ALL.ToString()))
        {
            var league = (await _leagueService.FindAsync(leagueOption)).FirstOrDefault();
            if (league == null) return (null, leagueOption);
            else leagues.Add(league);
        }
        else
            leagues.AddRange(await _leagueService.GetAllAsync());

        return (leagues, leagueOption);
    }

    private async Task<(IEnumerable<Team>?, string?)> GetTeamAsync(SocketSlashCommandDataOption cmd)
    {
        var teamOption = cmd.Options.FirstOrDefault(o => "team".Equals(o.Name))?.Value?.ToString();
        if (teamOption == null) return (null, teamOption);

        var teams = new List<Team>();

        if (!teamOption.Equals(TYPE_ALL.ToString()))
        {
            var team = (await _teamService.FindAsync(teamOption)).FirstOrDefault();
            if (team == null) return (null, teamOption);
            else teams.Add(team);
        }
        else
            teams.AddRange(await _teamService.GetAllAsync());

        return (teams, teamOption);
    }

    private Task<(IEnumerable<WatcherType>?, string?)> GetWatcherTypeAsync(SocketSlashCommandDataOption cmd)
    {
        var watcherType = cmd.Options.FirstOrDefault(o => "type".Equals(o.Name))?.Value?.ToString();
        if (watcherType == null) return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((null, watcherType));

        var watcherTypes = new List<WatcherType>();

        if (Enum.TryParse<WatcherType>(watcherType, out var watcherTypeEnum))
            watcherTypes.Add(watcherTypeEnum);
        else if (watcherType.Equals(TYPE_ALL_NEWS.ToString(), StringComparison.OrdinalIgnoreCase))
            watcherTypes.AddRange(Enum.GetValues<WatcherType>().Where(type => type != WatcherType.Games));
        else if (watcherType.Equals(TYPE_ALL.ToString(), StringComparison.OrdinalIgnoreCase))
            watcherTypes.AddRange(Enum.GetValues<WatcherType>());
        else
            watcherTypes = null;

        return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((watcherTypes, watcherType));
    }
}