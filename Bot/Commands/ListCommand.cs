using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Duthie.Bot.Extensions;
using Duthie.Bot.Utils;
using Duthie.Services.Leagues;
using Duthie.Services.Sites;
using Duthie.Services.Teams;
using Duthie.Types.Leagues;
using Duthie.Types.Sites;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class ListCommand : BaseCommand
{
    private const string ROLE_OWNER = "Server Owner";
    private const string ROLE_ADMINISTRATOR = "Server Administrator";

    private readonly ILogger<ListCommand> _logger;
    private readonly AppInfo _appInfo;
    private readonly SiteService _siteService;
    private readonly LeagueService _leagueService;
    private readonly TeamService _teamService;
    private readonly AdminCommand _adminCommand;
    private readonly WatcherCommand _watchersCommand;

    public ListCommand(
        ILogger<ListCommand> logger,
        AppInfo appInfo,
        SiteService siteService,
        LeagueService leagueService,
        TeamService teamService,
        AdminCommand adminCommand,
        WatcherCommand watchersCommand,
        DiscordConfiguration config) : base(config)
    {
        _logger = logger;
        _appInfo = appInfo;
        _siteService = siteService;
        _leagueService = leagueService;
        _teamService = teamService;
        _adminCommand = adminCommand;
        _watchersCommand = watchersCommand;
    }

    protected override string Command { get => "list"; }

    public override async Task<SlashCommandOptionBuilder> BuildAsync() =>
        new SlashCommandOptionBuilder()
            .WithName(Command)
            .WithDescription($"View data related to {_appInfo.Name}.")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(await BuildAdminsAsync())
            .AddOption(await BuildLeaguesAsync())
            .AddOption(await BuildSitesAsync())
            .AddOption(await BuildTeamsAsync())
            .AddOption(await BuildWatchersAsync())
            .AddOption(await BuildWatcherTypesAsync());

    private async Task<SlashCommandOptionBuilder> BuildAdminsAsync() =>
        (await _adminCommand.BuildListAsync())
            .WithName("admins")
            .WithDescription($"List the {_appInfo.Name} administrators for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

    private async Task<SlashCommandOptionBuilder> BuildLeaguesAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("leagues")
            .WithDescription($"List the leagues supported by {_appInfo.Name}.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddSiteFilter(cmd);
        await AddTagsFilter(cmd);
        return cmd;
    }

    private async Task<SlashCommandOptionBuilder> BuildSitesAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("sites")
            .WithDescription($"List the sites supported by {_appInfo.Name}.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddTagsFilter(cmd);
        return cmd;
    }

    private async Task<SlashCommandOptionBuilder> BuildTeamsAsync()
    {
        var cmd = new SlashCommandOptionBuilder()
            .WithName("teams")
            .WithDescription($"List the teams supported by {_appInfo.Name}.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        await AddSiteFilter(cmd);
        await AddLeagueFilter(cmd);
        await AddTagsFilter(cmd);
        return cmd;
    }

    private async Task<SlashCommandOptionBuilder> BuildWatchersAsync() =>
        (await _watchersCommand.BuildListAsync())
            .WithName("watchers")
            .WithDescription($"List the {_appInfo.Name} watchers registered to your server.")
            .WithType(ApplicationCommandOptionType.SubCommand);

    private Task<SlashCommandOptionBuilder> BuildWatcherTypesAsync() =>
        Task.FromResult(new SlashCommandOptionBuilder()
            .WithName("watcher-types")
            .WithDescription($"List the watcher types supported by {_appInfo.Name}.")
            .WithType(ApplicationCommandOptionType.SubCommand));

    protected override async Task HandleCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var cmd = command.Data.Options.First().Options.First();

            switch (cmd.Name)
            {
                case "admins":
                    await _adminCommand.ListAdminsAsync(command);
                    break;

                case "leagues":
                    await ListLeaguesAsync(command, cmd);
                    break;

                case "sites":
                    await ListSitesAsync(command, cmd);
                    break;

                case "teams":
                    await ListTeamsAsync(command, cmd);
                    break;

                case "watchers":
                    await _watchersCommand.ListWatchersAsync(command, cmd);
                    break;

                case "watcher-types":
                    await ListWatcherTypes(command);
                    break;

                default:
                    await SendUnrecognizedAsync(command);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error has occurred while handling list command.");
            await SendErrorAsync(command);
        }
    }

    private async Task ListLeaguesAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var (site, siteOption) = await GetSiteAsync(cmd);
        var (tags, tagsOption) = await GetTagsAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but that site doesn't seem to exist anymore.", ephemeral: true);
            return;
        }

        var leagues = await _leagueService.FindAsync(
            sites: site == null ? null : new Guid[] { site.Id },
            tags: tags);

        if (leagues.Count() > 0)
        {
            await command.RespondAsync(ListUtils.DrawBox(
                headers: new string[] {
                    "Name",
                    "Short Name",
                    "Site",
                    "Tags"
                },
                data: leagues.Select(l => new string[] {
                    l.Name,
                    l.ShortName,
                    l.Site.Name,
                    string.Join(", ", l.Tags)
                })), ephemeral: true);

            _logger.LogTrace($"User {user} viewed league list in guild \"{guild.Name}\" [{guild.Id}]");
        }
        else
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find any supported leagues for you.", ephemeral: true);
    }

    private async Task ListSitesAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var (tags, tagsOption) = await GetTagsAsync(cmd);

        var sites = await _siteService.FindAsync(
            tags: tags);

        if (sites.Count() > 0)
        {
            await command.RespondAsync(ListUtils.DrawBox(
                headers: new string[] {
                    "Name",
                    "URL",
                    "Tags"
                },
                data: sites.Select(s => new string[] {
                    s.Name,
                    s.Url,
                    string.Join(", ", s.Tags)
                })), ephemeral: true);

            _logger.LogTrace($"User {user} viewed site list in guild \"{guild.Name}\" [{guild.Id}]");
        }
        else
            await command.RespondAsync("I don't currently have any supported sites.", ephemeral: true);
    }

    private async Task ListTeamsAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var (league, leagueOption) = await GetLeagueAsync(cmd);
        var (site, siteOption) = await GetSiteAsync(cmd);
        var (tags, tagsOption) = await GetTagsAsync(cmd);

        if (siteOption != null && site == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but that site doesn't seem to exist anymore.", ephemeral: true);
            return;
        }

        if (leagueOption != null && league == null)
        {
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but that league doesn't seem to exist anymore.", ephemeral: true);
            return;
        }

        var teams = await _teamService.FindAsync(
            sites: site == null ? null : new Guid[] { site.Id },
            leagues: league == null ? null : new Guid[] { league.Id },
            tags: tags);

        if (teams.Count() > 0)
        {
            await command.RespondAsync(ListUtils.DrawBox(
                headers: new string[] {
                    "Name",
                    "Short Name",
                    "Leagues",
                    "Tags"
                },
                data: teams.Select(t => new string[] {
                    t.Name,
                    t.ShortName,
                    Regex.Replace(string.Join(", ", t.Leagues.Select(l => l.Name).OrderBy(n => n)), @"^(.{27}).{4,}", @"$1..."),
                    string.Join(", ", t.Tags)
                })), ephemeral: true);

            _logger.LogTrace($"User {user} viewed team list in guild \"{guild.Name}\" [{guild.Id}]");
        }
        else
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find any supported teams for you.", ephemeral: true);
    }

    private async Task ListWatcherTypes(SocketSlashCommand command)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        var watcherTypes = Enum.GetValues<WatcherType>();

        if (watcherTypes.Count() > 0)
        {
            await command.RespondAsync(ListUtils.DrawBox(
                headers: new string[] {
                    "Name",
                    "Description"
                },
                data: watcherTypes.Select(t => new string[] {
                    EnumUtils.GetName(t),
                    EnumUtils.GetDescription(t)
                })), ephemeral: true);

            _logger.LogTrace($"User {user} viewed watcher type list in guild \"{guild.Name}\" [{guild.Id}]");
        }
        else
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but I couldn't find any supported teams for you.", ephemeral: true);
    }

    private async Task<(League?, string?)> GetLeagueAsync(SocketSlashCommandDataOption cmd)
    {
        var leagueOption = cmd.Options.FirstOrDefault(o => "league" == o.Name)?.Value?.ToString();
        return (
            leagueOption == null ? null : (await _leagueService.FindAsync(leagueOption)).FirstOrDefault(),
            leagueOption
        );
    }

    private async Task<(Site?, string?)> GetSiteAsync(SocketSlashCommandDataOption cmd)
    {
        var siteOption = cmd.Options.FirstOrDefault(o => "site" == o.Name)?.Value?.ToString();
        return (
            siteOption == null ? null : (await _siteService.FindAsync(siteOption)).FirstOrDefault(),
            siteOption
        );
    }

    private Task<(string[]?, string?)> GetTagsAsync(SocketSlashCommandDataOption cmd)
    {
        var tagsOption = cmd.Options.FirstOrDefault(o => "tags" == o.Name)?.Value?.ToString();
        return Task.FromResult<(string[]?, string?)>((
            tagsOption == null ? null : Regex.Split(tagsOption, ",").Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim().ToLower()).ToArray(),
            tagsOption
        ));
    }
}