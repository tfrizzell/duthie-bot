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
            .WithDescription($"Manage {_appInfo.Name} administrators for your server.")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(await BuildAddAsync())
            .AddOption(await BuildListAsync())
            .AddOption(await BuildRemoveAsync())
            .AddOption(await BuildRemoveAllAsync());

    private Task<SlashCommandOptionBuilder> BuildAddAsync() =>
        Task.FromResult(new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription($"Add a {_appInfo.Name} administrator for your server.")
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

    private Task<SlashCommandOptionBuilder> BuildRemoveAllAsync() =>
        Task.FromResult(new SlashCommandOptionBuilder()
            .WithName("remove-all")
            .WithDescription($"Remove all {_appInfo.Name} administrators for your server.")
            .WithType(ApplicationCommandOptionType.SubCommand));

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

                case "remove-all":
                    await RemoveAllAdminsAsync(command, cmd);
                    break;

                default:
                    await SendUnrecognizedAsync(command);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error has occurred while handling admin command.");
            await SendErrorAsync(command);
        }
    }

    private async Task AddAdminAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        if (!await CheckPrivileges(command))
            return;

        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var targetUser = await GetTargetUserAsync(cmd);

        await SendResponseAsync(command, async () =>
        {
            if ((await _guildAdminService.SaveAsync(guild.Id, targetUser.Id)) > 0)
            {
                _logger.LogDebug($"User {user} added administrator {targetUser} to guild \"{guild.Name}\" [{guild.Id}]");
                await command.RespondAsync($"Okay! I've added {targetUser} as a {_appInfo.Name} administrator for your server.", ephemeral: true);
            }
            else
                await command.RespondAsync($"{targetUser} is already a {_appInfo.Name} administrator for your server.", ephemeral: true);
        });
    }

    public async Task ListAdminsAsync(SocketSlashCommand command)
    {
        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);

        await SendResponseAsync(command, async () =>
        {
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

            await command.RespondAsync(ListUtils.CreateTable(
                headers: new string[] {
                    "User",
                    "Access Level"
                },
                data: admins.Values
                    .OrderBy(a => ROLE_OWNER == a[1])
                        .ThenBy(a => ROLE_ADMINISTRATOR == a[1])
                        .ThenBy(a => a[0])), ephemeral: true);

            _logger.LogTrace($"User {user} viewed administrator list for guild \"{guild.Name}\" [{guild.Id}]");
        }, "I'll have the admin list for you shortly.");
    }

    private async Task RemoveAllAdminsAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        if (!await CheckPrivileges(command))
            return;

        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var targetUser = await GetTargetUserAsync(cmd);

        await SendResponseAsync(command, async () =>
        {
            var count = await _guildAdminService.DeleteAsync(guild.Id, (await _guildAdminService.GetAllAsync(guild.Id)).ToArray());

            if (count > 0)
            {
                _logger.LogDebug($"User {user} removed {MessageUtils.Pluralize(count, "administrator")} from guild \"{guild.Name}\" [{guild.Id}]");
                await command.RespondAsync($"Okay! I've removed {MessageUtils.Pluralize(count, $"{_appInfo.Name} administrator")} for your server.", ephemeral: true);
            }
            else
                await command.RespondAsync($"Okay! There were no {_appInfo.Name} administrators to remove.", ephemeral: true);
        });
    }

    private async Task RemoveAdminAsync(SocketSlashCommand command, SocketSlashCommandDataOption cmd)
    {
        if (!await CheckPrivileges(command))
            return;

        var guild = await GetGuildAsync(command);
        var user = await GetUserAsync(command);
        var targetUser = await GetTargetUserAsync(cmd);

        await SendResponseAsync(command, async () =>
        {
            if ((await _guildAdminService.DeleteAsync(guild.Id, targetUser.Id)) > 0)
            {
                _logger.LogDebug($"User {user} removed administrator {targetUser} from guild \"{guild.Name}\" [{guild.Id}]");
                await command.RespondAsync($"Okay! I've removed {targetUser} as a {_appInfo.Name} administrator for your server.", ephemeral: true);
            }
            else
                await command.RespondAsync($"{targetUser} is not a {_appInfo.Name} administrator for your server.", ephemeral: true);
        });
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
        Task.FromResult((SocketGuildUser)cmd.Options.First(o => o.Name == "user").Value);
}