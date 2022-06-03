using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Duthie.Bot.Extensions;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class CommandRegistrationService
{
    private readonly ILogger<CommandRegistrationService> _logger;
    private readonly AppInfo _appInfo;
    private readonly IEnumerable<ICommand> _commands;

    public CommandRegistrationService(
        ILogger<CommandRegistrationService> logger,
        AppInfo appInfo,
        IEnumerable<ICommand> commands)
    {
        _logger = logger;
        _appInfo = appInfo;
        _commands = commands;
    }

    public async Task<ApplicationCommandProperties[]> GetCommands()
    {
        var commands = new List<ApplicationCommandProperties>();

        var command = new SlashCommandBuilder()
            .WithName("duthie")
            .WithDescription($"Use `/duthie` commants to communicate with {_appInfo.Name}.");

        foreach (var cmd in _commands)
            command.AddOption(await cmd.BuildAsync());

        commands.Add(command.Build());
        return commands.ToArray();
    }

    public async Task RegisterCommandsAsync(BaseSocketClient client)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Registering slash commands");
        await client.ShowRegisteringStatusAsync();

        try
        {
            var commands = await GetCommands();

            foreach (var guild in client.Guilds)
                await guild.BulkOverwriteApplicationCommandAsync(commands);

            sw.Stop();

            _logger.LogDebug($"Command registration completed in {sw.Elapsed.TotalMilliseconds} milliseconds");
        }
        catch (Exception e)
        {
            sw.Stop();

            _logger.LogError(e, $"Command registration failed in {sw.Elapsed.TotalMilliseconds} milliseconds");
            Environment.Exit(0);
        }
    }

    public async Task RegisterCommandsAsync(SocketGuild guild)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug($"Registering slash commands to guild \"{guild.Name}\" [{guild.Id}]");

        try
        {
            await guild.BulkOverwriteApplicationCommandAsync(await GetCommands());
            sw.Stop();

            _logger.LogDebug($"Command registration completed in {sw.Elapsed.TotalMilliseconds} milliseconds");
        }
        catch (Exception e)
        {
            sw.Stop();

            _logger.LogError(e, $"Command registration failed in {sw.Elapsed.TotalMilliseconds} milliseconds");
            Environment.Exit(0);
        }
    }
}