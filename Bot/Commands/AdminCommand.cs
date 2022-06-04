using Discord;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Duthie.Bot.Extensions;
using Duthie.Bot.Utils;
using Duthie.Services.Guilds;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class AdminCommand : BaseCommand
{
    private const string ROLE_OWNER = "Server Owner";
    private const string ROLE_ADMINISTRATOR = "Server Administrator";

    private readonly ILogger<AdminCommand> _logger;
    private readonly AppInfo _appInfo;
    private readonly GuildAdminService _guildAdminService;

    public AdminCommand(
        ILogger<AdminCommand> logger,
        AppInfo appInfo,
        GuildAdminService guildAdminService,
        DiscordConfiguration config) : base(config)
    {
        _logger = logger;
        _appInfo = appInfo;
        _guildAdminService = guildAdminService;
    }

    protected override string Command { get => "admin"; }

    public override async Task<SlashCommandOptionBuilder> BuildAsync() =>
        new SlashCommandOptionBuilder()
            .WithName(Command)
            .WithDescription($"Add, remove, or view users as {_appInfo.Name} administrators for your server.")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(await BuildAddAsync())
            .AddOption(await BuildListAsync())
            .AddOption(await BuildRemoveAsync());

    private Task<SlashCommandOptionBuilder> BuildAddAsync() =>
        Task.FromResult(new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription($"Add a new {_appInfo.Name} administrator for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("user", ApplicationCommandOptionType.User, "the user to give administrator access to", isRequired: true));

    public Task<SlashCommandOptionBuilder> BuildListAsync() =>
        Task.FromResult(new SlashCommandOptionBuilder()
            .WithName("list")
            .WithDescription($"View the {_appInfo.Name} administrators for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand));

    private Task<SlashCommandOptionBuilder> BuildRemoveAsync() =>
        Task.FromResult(new SlashCommandOptionBuilder()
            .WithName("remove")
            .WithDescription($"Remove a {_appInfo.Name} administrator for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("user", ApplicationCommandOptionType.User, "the user to remove administrator acess from", isRequired: true));

    protected override async Task HandleCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var cmd = command.Data.Options.First().Options.First();

            switch (cmd.Name)
            {
                case "add":
                    await AddAdminAsync(command, cmd);
                    break;

                case "list":
                    await ListAdminsAsync(command);
                    break;

                case "remove":
                    await RemoveAdminAsync(command, cmd);
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

    private async Task AddAdminAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await CheckPrivileges(command))
            return;

        var targetUser = await GetTargetUserAsync(cmd);
        _logger.LogDebug($"User {user} added administrator {targetUser} to guild \"{guild.Name}\" [{guild.Id}]");

        if ((await _guildAdminService.SaveAsync(guild.Id, targetUser.Id)) > 0)
            await command.RespondAsync($"Okay! I've added {targetUser} as a {_appInfo.Name} administrator for your server.", ephemeral: true);
        else
            await command.RespondAsync($"{targetUser} is already a {_appInfo.Name} administrator for your server.", ephemeral: true);
    }

    public async Task ListAdminsAsync(SocketSlashCommand command)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        _logger.LogDebug($"User {user} viewed administrator list for guild \"{guild.Name}\" [{guild.Id}]");

        var admins = new Dictionary<ulong, string[]>()
        {
            [guild.Owner.Id] = new string[] {
                $"{guild.Owner.Username}@{guild.Owner.Discriminator}",
                ROLE_OWNER
            },
        };

        foreach (var member in guild.Users.Where(m => m.GuildPermissions.Administrator && m.Id != guild.OwnerId).OrderBy(m => m.Mention))
        {
            if (!admins.ContainsKey(member.Id))
                admins.Add(member.Id, new string[] {
                    $"{member.Username}@{member.Discriminator}",
                    ROLE_ADMINISTRATOR
                });
        }

        foreach (var memberId in await _guildAdminService.GetAllAsync(guild.Id))
        {
            if (!admins.ContainsKey(memberId))
            {
                var member = guild.Users.FirstOrDefault(m => m.Id == memberId);

                if (member != null)
                    admins.Add(member.Id, new string[] {
                        $"{member.Username}@{member.Discriminator}",
                        $"{_appInfo.Name} Administrator"
                    });
            }
        }

        await command.RespondAsync(ListUtils.DrawBox(
            headers: new string[] {
                "User",
                "Access Level"
            },
            data: admins.Values
                .OrderBy(a => ROLE_OWNER.Equals(a[1]))
                .ThenBy(a => ROLE_ADMINISTRATOR.Equals(a[1]))
                .ThenBy(a => a[0])), ephemeral: true);
    }

    private async Task RemoveAdminAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await CheckPrivileges(command))
            return;

        var targetUser = await GetTargetUserAsync(cmd);
        _logger.LogDebug($"User {user} removed administrator {targetUser} from guild \"{guild.Name}\" [{guild.Id}]");

        if ((await _guildAdminService.DeleteAsync(guild.Id, targetUser.Id)) > 0)
            await command.RespondAsync($"Okay! I've removed {targetUser} as a {_appInfo.Name} administrator for your server.", ephemeral: true);
        else
            await command.RespondAsync($"{targetUser} is not a {_appInfo.Name} administrator for your server.", ephemeral: true);
    }

    private async Task<bool> CheckPrivileges(SocketSlashCommand command)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        if (!await IsOwnerAsync(user) && !await IsAdministratorAsync(user))
        {
            _logger.LogWarning($"Admin command issued in guild \"{guild.Name}\" [{guild.Id}] by non-administrator {command.User}");
            await command.RespondAsync($"I'm sorry {command.User.Mention}, but you don't have permission to do that!", ephemeral: true);
            return false;
        }

        return true;
    }

    private Task<SocketGuildUser> GetTargetUserAsync(SocketSlashCommandDataOption cmd) =>
        Task.FromResult((SocketGuildUser)cmd.Options.First(o => "user" == o.Name).Value);
}