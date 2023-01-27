using System.Diagnostics;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Duthie.Bot.Extensions;
using Duthie.Services.Guilds;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class CommandRegistrationService
{
    private readonly ILogger<CommandRegistrationService> _logger;
    private readonly AppInfo _appInfo;
    private readonly IEnumerable<ICommand> _commands;
    private readonly GuildService _guildService;

    public CommandRegistrationService(
        ILogger<CommandRegistrationService> logger,
        AppInfo appInfo,
        IEnumerable<ICommand> commands,
        GuildService guildService)
    {
        _logger = logger;
        _appInfo = appInfo;
        _commands = commands;
        _guildService = guildService;
    }

    public async Task<ApplicationCommandProperties[]> GetCommands()
    {
        var commands = new List<ApplicationCommandProperties>();

        var command = new SlashCommandBuilder()
            .WithName("duthie")
            .WithDescription($"Use `/duthie` commands to communicate with {_appInfo.Name}.");

        foreach (var cmd in _commands)
            command.AddOption(await cmd.BuildAsync());

        commands.Add(command.Build());
        return commands.ToArray();
    }

    public async Task RegisterCommandsAsync(BaseSocketClient client)
    {
        _logger.LogDebug("Registering commands");
        var sw = Stopwatch.StartNew();

        try
        {
            await client.ShowRegisteringStatusAsync();

            var commands = await GetCommands();

            foreach (var guild in client.Guilds)
            {
                try
                {
                    await guild.BulkOverwriteApplicationCommandAsync(commands);
                }
                catch (HttpException ex) when (ex.Message.EndsWith("error 50001: Missing Access"))
                {
                    _logger.LogDebug($"Unable to register commands in guild \"{guild.Name}\" [{guild.Id}]");
                    await NotifyOwner(client, guild);
                }
            }

            sw.Stop();
            _logger.LogTrace($"Command registration completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Command registration failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during command registration.");
            Environment.Exit(ExitCode.CommandRegistrationFailure);
        }
    }

    public async Task RegisterCommandsAsync(BaseSocketClient client, SocketGuild guild)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug($"Registering commands to guild \"{guild.Name}\" [{guild.Id}]");

        try
        {
            try
            {
                await guild.BulkOverwriteApplicationCommandAsync(await GetCommands());
            }
            catch (HttpException ex) when (ex.Message.EndsWith("error 50001: Missing Access"))
            {
                _logger.LogDebug($"Unable to register commands in guild \"{guild.Name}\" [{guild.Id}]");
                await NotifyOwner(client, guild);
            }

            sw.Stop();
            _logger.LogDebug($"Command registration completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();

            _logger.LogError(e, $"Command registration failed in {sw.Elapsed.TotalSeconds}s");
            Environment.Exit(ExitCode.CommandRegistrationFailure);
        }
    }

    private async Task NotifyOwner(BaseSocketClient client, SocketGuild guild)
    {
        var _guild = await _guildService.GetAsync(guild.Id);

        if (_guild?.CommandNotificationSent == false)
        {
            var owner = guild.Owner ?? (IUser?)client.GetUser(guild.OwnerId) ?? await client.Rest.GetUserAsync(guild.OwnerId);
            await owner.SendMessageAsync($"**!!! IMPORTANT !!!**\nDuthie Bot has been upgraded to version 3. However, it has been unable to register its new commands to your server: *{guild.Name}*\n\nPlease remove Duthie Both from the server, and invite it back using the following link:\n- <https://discord.com/api/oauth2/authorize?client_id=356076231185268746&permissions=2048&scope=bot%20applications.commands>\n\nIf you receive this message again after re-inviting it, please report a bug at <https://github.com/tfrizzell/duthie-bot/issues> if one doesn't already exist.\n\nThanks!");

            _guild.CommandNotificationSent = true;
            await _guildService.SaveAsync(_guild);
        }
    }
}