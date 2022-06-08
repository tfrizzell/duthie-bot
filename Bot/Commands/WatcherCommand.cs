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
using Duthie.Types.Sites;
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

        await AddSiteFilter(cmd);
        await AddLeagueOption(cmd, true);
        await AddTeamOption(cmd, true);
        await AddTypeOption(cmd, true);
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

        await AddSiteFilter(cmd);
        await AddLeagueOption(cmd, false);
        await AddTeamOption(cmd, false);
        await AddTypeOption(cmd, false);
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

    private Task AddTypeOption(SlashCommandOptionBuilder cmd, bool required)
    {
        var types = Enum.GetValues<WatcherType>();

        if (types.Count() == 0)
            return Task.CompletedTask;

        var typeOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("type")
            .WithDescription("the type of watchers to register")
            .WithRequired(required);

        typeOption.AddChoice("All", TYPE_ALL.ToString());
        typeOption.AddChoice("All News", TYPE_ALL_NEWS.ToString());

        foreach (var type in types)
            typeOption.AddChoice(EnumUtils.GetName(type), type.ToString());

        cmd.AddOption(typeOption);
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

        var (site, siteOption) = await GetSiteAsync(cmd);
        var (leagues, leagueOption) = await GetLeaguesAsync(cmd, returnAllTeams: true);
        var (teams, teamOption) = await GetTeamsAsync(cmd, returnAllTeams: true);
        var (types, typeOption) = await GetTypesAsync(cmd);
        var channel = await GetChannelAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
            return;
        }

        if (leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        if (teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }

        if (types == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{typeOption}`.", ephemeral: true);
            return;
        }

        var watchers = leagues.Where(l => site == null || l.SiteId == site.Id)
            .Select(league =>
                teams.Where(t => league.LeagueTeams.Any(lt => lt.TeamId == t.Id))
                    .Select(team =>
                        types.Select(type =>
                            new Watcher
                            {
                                GuildId = guild.Id,
                                LeagueId = league.Id,
                                TeamId = team.Id,
                                Type = type,
                                ChannelId = channel?.Id,
                            }))
                    .SelectMany(w => w))
            .SelectMany(w => w);

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

        var (site, siteOption) = await GetSiteAsync(cmd);
        var (leagues, leagueOption) = await GetLeaguesAsync(cmd);
        var (teams, teamOption) = await GetTeamsAsync(cmd);
        var (types, typeOption) = await GetTypesAsync(cmd);
        var channel = await GetChannelAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
            return;
        }

        if (leagueOption != null && leagueOption.ToLower() != "all" && leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        if (teamOption != null && teamOption.ToLower() != "all" && teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }

        if (typeOption != null && types == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{typeOption}`.", ephemeral: true);
            return;
        }

        var watchers = await _watcherService.FindAsync(guild.Id,
            leagues: leagues == null ? null : leagues.Where(l => site == null || l.SiteId == site.Id).Select(l => l.Id),
            teams: teams == null ? null : teams.Select(t => t.Id),
            types: types,
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
        bool.TryParse(cmd.Options.FirstOrDefault(o => o.Name == "all")?.Value?.ToString() ?? "", out removeAll);

        if (!removeAll)
        {
            var (site, siteOption) = await GetSiteAsync(cmd);
            var (leagues, leagueOption) = await GetLeaguesAsync(cmd);
            var (teams, teamOption) = await GetTeamsAsync(cmd);
            var (types, typeOption) = await GetTypesAsync(cmd);
            var channel = await GetChannelAsync(cmd);

            if (siteOption != null && site == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
                return;
            }

            if (leagueOption != null && leagueOption.ToLower() != "all" && leagues == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
                return;
            }

            if (teamOption != null && teamOption.ToLower() != "all" && teams == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the teams `{teamOption}`.", ephemeral: true);
                return;
            }

            if (typeOption != null && types == null)
            {
                await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher types `{typeOption}`.", ephemeral: true);
                return;
            }

            watchers.AddRange(await _watcherService.FindAsync(guild.Id,
                leagues: leagues == null ? null : leagues.Where(l => site == null || l.SiteId == site.Id).Select(l => l.Id),
                teams: teams == null ? null : teams.Select(t => t.Id),
                types: types,
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
        Task.FromResult((SocketTextChannel?)cmd.Options.FirstOrDefault(o => o.Name == "channel")?.Value);

    private async Task<(IEnumerable<League>?, string?)> GetLeaguesAsync(SocketSlashCommandDataOption cmd, bool returnAllTeams = false)
    {
        var leagueOption = cmd.Options.FirstOrDefault(o => o.Name == "league")?.Value?.ToString();
        if (leagueOption == null) return (null, leagueOption);

        var leagues = new List<League>();

        if (leagueOption.ToLower() != "all")
        {
            var league = (await _leagueService.FindAsync(leagueOption)).FirstOrDefault();
            if (league == null) return (null, leagueOption);
            leagues.Add(league);
        }
        else if (!returnAllTeams)
            return (null, leagueOption);
        else
            leagues.AddRange(await _leagueService.GetAllAsync());

        return (leagues, leagueOption);
    }

    private async Task<(Site?, string?)> GetSiteAsync(SocketSlashCommandDataOption cmd)
    {
        var siteOption = cmd.Options.FirstOrDefault(o => o.Name == "site")?.Value?.ToString();
        return (
            siteOption == null ? null : (await _siteService.FindAsync(siteOption)).FirstOrDefault(),
            siteOption
        );
    }

    private async Task<(IEnumerable<Team>?, string?)> GetTeamsAsync(SocketSlashCommandDataOption cmd, bool returnAllTeams = false)
    {
        var teamOption = cmd.Options.FirstOrDefault(o => o.Name == "team")?.Value?.ToString();
        if (teamOption == null) return (null, teamOption);

        var teams = new List<Team>();

        if (teamOption.ToLower() != "all")
        {
            var team = (await _teamService.FindAsync(teamOption)).FirstOrDefault();
            if (team == null) return (null, teamOption);
            teams.Add(team);
        }
        else if (!returnAllTeams)
            return (null, teamOption);
        else
            teams.AddRange(await _teamService.GetAllAsync());

        return (teams, teamOption);
    }

    private Task<(IEnumerable<WatcherType>?, string?)> GetTypesAsync(SocketSlashCommandDataOption cmd)
    {
        var typeOption = cmd.Options.FirstOrDefault(o => o.Name == "type")?.Value?.ToString();
        if (typeOption == null) return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((null, typeOption));

        var types = new List<WatcherType>();

        if (TYPE_ALL.ToString() == typeOption)
            types.AddRange(Enum.GetValues<WatcherType>());
        else if (TYPE_ALL_NEWS.ToString() == typeOption)
            types.AddRange(Enum.GetValues<WatcherType>().Where(type => type != WatcherType.Games));
        else if (Enum.TryParse<WatcherType>(typeOption, out var typeEnum))
            types.Add(typeEnum);
        else
            types = null;

        return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((types, typeOption));
    }
}