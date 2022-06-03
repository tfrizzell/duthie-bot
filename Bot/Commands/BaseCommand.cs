using Discord;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Duthie.Bot.Utils;
using Duthie.Services.Guilds;
using Duthie.Types.Watchers;

namespace Duthie.Bot.Commands;

public abstract class BaseCommand : ICommand
{
    private readonly DiscordConfiguration _config;

    protected BaseCommand(DiscordConfiguration config)
    {
        _config = config;
    }

    protected abstract string Command { get; }

    public abstract Task<SlashCommandOptionBuilder> BuildAsync();

    protected Task AddChannelFilter(SlashCommandOptionBuilder cmd)
    {
        var channelOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.Channel)
            .WithName("channel")
            .WithDescription("the channel to filter by");

        cmd.AddOption(channelOption);
        return Task.CompletedTask;
    }

    protected Task AddLeagueFilter(SlashCommandOptionBuilder cmd)
    {
        var leagueOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("league")
            .WithDescription("the league to filter by");

        cmd.AddOption(leagueOption);
        return Task.CompletedTask;
    }

    protected Task AddSiteFilter(SlashCommandOptionBuilder cmd)
    {
        var siteOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("site")
            .WithDescription("the site to filter by");

        cmd.AddOption(siteOption);
        return Task.CompletedTask;
    }

    protected Task AddTagsFilter(SlashCommandOptionBuilder cmd)
    {
        var tagsOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("tags")
            .WithDescription("a comma-separated list of tags to filter by");

        cmd.AddOption(tagsOption);
        return Task.CompletedTask;
    }

    protected Task AddTeamFilter(SlashCommandOptionBuilder cmd)
    {
        var teamOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("team")
            .WithDescription("the team to filter by");

        cmd.AddOption(teamOption);
        return Task.CompletedTask;
    }

    protected Task AddWatcherTypeFilter(SlashCommandOptionBuilder cmd)
    {
        var watcherTypes = Enum.GetValues<WatcherType>();

        if (watcherTypes.Count() == 0)
            return Task.CompletedTask;

        var watcherTypeOption = new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.String)
            .WithName("type")
            .WithDescription("the watcher type to filter by");

        watcherTypeOption.AddChoice("All", "ALL");
        watcherTypeOption.AddChoice("All News", "ALL_NEWS");

        foreach (var watcherType in watcherTypes)
            watcherTypeOption.AddChoice(EnumUtils.GetName(watcherType), watcherType.ToString());

        cmd.AddOption(watcherTypeOption);
        return Task.CompletedTask;
    }

    public virtual async Task HandleAsync(SocketSlashCommand command)
    {
        if (!Command.Equals(command.Data.Options.FirstOrDefault()?.Name))
            return;

        await HandleCommandAsync(command);
    }

    protected virtual async Task HandleCommandAsync(SocketSlashCommand command)
    {
        await SendUnsupportedAsync(command);
    }

    protected async Task SendErrorAsync(SocketSlashCommand command) =>
        await command.RespondAsync($"Uh oh! It looks like something's gone wrong. Please report this issue to the developers.", ephemeral: true);

    protected async Task SendUnrecognizedAsync(SocketSlashCommand command) =>
        await command.RespondAsync($"I'm sorry {command.User.Mention}, but I didn't recognize that command.", ephemeral: true);

    protected async Task SendUnsupportedAsync(SocketSlashCommand command) =>
        await command.RespondAsync($"I'm sorry {command.User.Mention}, but that command isn't implemented yet.", ephemeral: true);

    protected Task<bool> IsOwnerAsync(SocketGuildUser user) =>
        Task.FromResult(user.Guild.OwnerId == user.Id);

    protected virtual Task<bool> IsAdministratorAsync(SocketGuildUser user) =>
        Task.FromResult(user.GuildPermissions.Administrator);

    protected static Task<SocketTextChannel> GetChannelAsync(SocketSlashCommand command) =>
        Task.FromResult((command.Channel as SocketTextChannel)!);

    protected static async Task<SocketGuild> GetGuildAsync(SocketSlashCommand command) =>
        (await GetChannelAsync(command)).Guild;

    protected static Task<SocketGuildUser> GetUserAsync(SocketSlashCommand command) =>
        Task.FromResult((command.User as SocketGuildUser)!);
}

public abstract class BaseCommandWithAdminCheck : BaseCommand
{
    private readonly GuildAdminService _guildAdminService;

    protected BaseCommandWithAdminCheck(
        DiscordConfiguration config,
        GuildAdminService guildAdminService) : base(config)
    {
        _guildAdminService = guildAdminService;
    }

    protected override async Task<bool> IsAdministratorAsync(SocketGuildUser user) =>
        await base.IsAdministratorAsync(user) || await _guildAdminService.ExistsAsync(user.Guild.Id, user.Id);
}