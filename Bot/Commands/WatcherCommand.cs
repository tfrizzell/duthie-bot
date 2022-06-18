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
            .WithDescription($"Manage {_appInfo.Name} watchers for your server.")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(await BuildAddAsync())
            .AddOption(await BuildListAsync())
            .AddOption(await BuildRemoveAsync())
            .AddOption(await BuildRemoveAllAsync());

    private async Task<SlashCommandOptionBuilder> BuildAddAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription($"Add a {_appInfo.Name} watcher to your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddSiteFilter(cmd);
        await AddLeagueOption(cmd, "the league you want to start watching");
        await AddTeamOption(cmd, "the team to start watching");
        await AddTypeOption(cmd, "the type of data you want to start watching");
        await AddChannelOption(cmd, "the channel you want to send messages to");
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
            .WithDescription($"Remove a {_appInfo.Name} watcher from your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddSiteFilter(cmd);
        await AddLeagueOption(cmd, "the league you want to stop watching");
        await AddTeamOption(cmd, "the team to stop watching");
        await AddTypeOption(cmd, "the type of data you want to stop watching");
        await AddChannelOption(cmd, "the channel you want to stop receiving messages on");
        return cmd;
    }

    private async Task<SlashCommandOptionBuilder> BuildRemoveAllAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("remove-all")
            .WithDescription($"Remove all {_appInfo.Name} watchers from your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddSiteFilter(cmd);
        await AddLeagueFilter(cmd);
        await AddTeamFilter(cmd);
        await AddWatcherTypeFilter(cmd);
        await AddChannelFilter(cmd);
        return cmd;
    }

    private Task AddChannelOption(SlashCommandOptionBuilder cmd, string description)
    {
        var channelOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.Channel)
            .WithName("channel")
            .WithDescription("the channel to send messages to");

        cmd.AddOption(channelOption);
        return Task.CompletedTask;
    }

    private Task AddLeagueOption(SlashCommandOptionBuilder cmd, string description)
    {
        var leagueOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("league")
            .WithDescription(description)
            .WithRequired(true);

        cmd.AddOption(leagueOption);
        return Task.CompletedTask;
    }

    private Task AddTeamOption(SlashCommandOptionBuilder cmd, string description)
    {
        var teamOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("team")
            .WithDescription(description)
            .WithRequired(true);

        cmd.AddOption(teamOption);
        return Task.CompletedTask;
    }

    private Task AddTypeOption(SlashCommandOptionBuilder cmd, string description)
    {
        var types = Enum.GetValues<WatcherType>();

        if (types.Count() == 0)
            return Task.CompletedTask;

        var typeOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("type")
            .WithDescription(description)
            .WithRequired(true);

        typeOption.AddChoice("All", WATCHER_TYPE_ALL.ToString());
        typeOption.AddChoice("All News", WATCHER_TYPE_ALL.ToString());

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

                case "remove-all":
                    await RemoveAllWatchersAsync(command, cmd);
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
        if (!await CheckPrivileges(command))
            return;

        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var (site, siteOption) = await GetSiteAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
            return;
        }

        var (leagues, leagueOption) = await GetLeaguesAsync(cmd, returnAllLeagues: true);

        if (leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        var (teams, teamOption) = await GetTeamsAsync(cmd, returnAllTeams: true);

        if (teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }

        var (types, typeOption) = await GetTypesAsync(cmd);

        if (types == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{typeOption}`.", ephemeral: true);
            return;
        }

        var channel = await GetChannelAsync(cmd);

        await SendResponseAsync(command, async () =>
        {
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
        });
    }

    public async Task ListWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        var (site, siteOption) = await GetSiteAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
            return;
        }

        var (leagues, leagueOption) = await GetLeaguesAsync(cmd);

        if (leagueOption != null && leagueOption.ToLower() != "all" && leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        var (teams, teamOption) = await GetTeamsAsync(cmd);

        if (teamOption != null && teamOption.ToLower() != "all" && teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }

        var (types, typeOption) = await GetTypesAsync(cmd);

        if (typeOption != null && types == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{typeOption}`.", ephemeral: true);
            return;
        }

        var channel = await GetChannelAsync(cmd);

        await SendResponseAsync(command, async () =>
        {
            var watchers = await _watcherService.FindAsync(guild.Id,
                sites: site == null ? null : new Guid[] { site.Id },
                leagues: leagues == null ? null : leagues.Select(l => l.Id),
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
        }, "I'll have the watcher list for you shortly.");
    }

    private async Task RemoveAllWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        if (!await CheckPrivileges(command))
            return;

        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var (site, siteOption) = await GetSiteAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
            return;
        }

        var (leagues, leagueOption) = await GetLeaguesAsync(cmd, returnAllLeagues: site != null);

        if (leagueOption != null && leagueOption.ToLower() != "all" && leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        var (teams, teamOption) = await GetTeamsAsync(cmd);

        if (teamOption != null && teamOption.ToLower() != "all" && teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the teams `{teamOption}`.", ephemeral: true);
            return;
        }

        var (types, typeOption) = await GetTypesAsync(cmd);

        if (typeOption != null && types == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher types `{typeOption}`.", ephemeral: true);
            return;
        }

        var channel = await GetChannelAsync(cmd);

        await RemoveWatchersAsync(command,
            await _watcherService.FindAsync(guild.Id,
                sites: site == null ? null : new Guid[] { site.Id },
                leagues: leagues == null ? null : leagues.Select(l => l.Id),
                teams: teams == null ? null : teams.Select(t => t.Id),
                types: types,
                channels: channel == null ? null : new List<ulong?>() { channel.Id }));
    }

    private async Task RemoveWatchersAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        if (!await CheckPrivileges(command))
            return;

        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var (site, siteOption) = await GetSiteAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the site `{siteOption}`.", ephemeral: true);
            return;
        }

        var (leagues, leagueOption) = await GetLeaguesAsync(cmd, returnAllLeagues: true);

        if (leagues == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the league `{leagueOption}`.", ephemeral: true);
            return;
        }

        var (teams, teamOption) = await GetTeamsAsync(cmd, returnAllTeams: true);

        if (teams == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the team `{teamOption}`.", ephemeral: true);
            return;
        }

        var (types, typeOption) = await GetTypesAsync(cmd);

        if (types == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find the watcher type `{typeOption}`.", ephemeral: true);
            return;
        }

        var channel = await GetChannelAsync(cmd);

        await RemoveWatchersAsync(command,
            await _watcherService.FindAsync(guild.Id,
                sites: site == null ? null : new Guid[] { site.Id },
                leagues: leagues.Select(l => l.Id),
                teams: teams.Select(t => t.Id),
                types: types,
                channels: channel == null ? null : new List<ulong?>() { channel.Id }));
    }

    private async Task RemoveWatchersAsync(SocketSlashCommand command, IEnumerable<Watcher> watchers)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        await SendResponseAsync(command, async () =>
        {
            var count = await _watcherService.DeleteAsync(watchers.Select(w => w.Id));

            if (count > 0)
            {
                _logger.LogDebug($"User {user} deleted {MessageUtils.Pluralize(count, "watcher")} from guild \"{guild.Name}\" [{guild.Id}]");
                await command.RespondAsync($"Okay! I've deleted {MessageUtils.Pluralize(count, "watcher")} from your server.", ephemeral: true);
            }
            else
                await command.RespondAsync($"Okay! There were no watchers to remove.", ephemeral: true);
        });
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

    private async Task<(IEnumerable<League>?, string?)> GetLeaguesAsync(SocketSlashCommandDataOption cmd, bool returnAllLeagues = false)
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
        else if (!returnAllLeagues)
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

        if (WATCHER_TYPE_ALL.ToString() == typeOption)
            types.AddRange(Enum.GetValues<WatcherType>());
        else if (WATCHER_TYPE_ALL_NEWS.ToString() == typeOption)
            types.AddRange(Enum.GetValues<WatcherType>().Where(type => type != WatcherType.Games));
        else if (Enum.TryParse<WatcherType>(typeOption, out var typeEnum))
            types.Add(typeEnum);
        else
            types = null;

        return Task.FromResult<(IEnumerable<WatcherType>?, string?)>((types, typeOption));
    }
}