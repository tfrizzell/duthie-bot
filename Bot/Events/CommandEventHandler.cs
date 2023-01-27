using Discord.WebSocket;
using Duthie.Bot.Commands;
using Duthie.Bot.Configuration;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Events;

public class CommandEventHandler : IAsyncHandler
{
    private readonly ILogger<CommandEventHandler> _logger;
    private readonly DiscordShardedClient _client;
    private readonly CommandRegistrationService _commandRegistrationService;
    private readonly IEnumerable<ICommand> _commands;
    private readonly DiscordConfiguration _config;
    private readonly AppInfo _appInfo;

    public CommandEventHandler(
        ILogger<CommandEventHandler> logger,
        DiscordShardedClient client,
        CommandRegistrationService commandRegistrationService,
        IEnumerable<ICommand> commands,
        DiscordConfiguration config,
        AppInfo appInfo)
    {
        _logger = logger;
        _client = client;
        _commandRegistrationService = commandRegistrationService;
        _commands = commands;
        _config = config;
        _appInfo = appInfo;
    }

    public ValueTask RunAsync()
    {
        _client.ShardReady += HandleReadyAsync;
        _client.JoinedGuild += HandleJoinedGuildAsync;
        _client.SlashCommandExecuted += HandleCommandAsync;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client.SlashCommandExecuted -= HandleCommandAsync;
        _client.JoinedGuild -= HandleJoinedGuildAsync;
        _client.ShardReady -= HandleReadyAsync;
        return ValueTask.CompletedTask;
    }

    private async Task HandleReadyAsync(DiscordSocketClient client)
    {
        _client.ShardReady -= HandleReadyAsync;
        await _commandRegistrationService.RegisterCommandsAsync(client);
    }

    private async Task HandleCommandAsync(SocketSlashCommand command)
    {
        if (command.User.IsBot && !_config.AcceptCommandsFromBots)
        {
            await command.RespondAsync("I'm sorry {command.User.Mention}, but I don't accept commands from other bots.", ephemeral: true);
            return;
        }

        foreach (var handler in _commands)
            await handler.HandleAsync(command);
    }

    private async Task HandleJoinedGuildAsync(SocketGuild guild)
    {
        await _commandRegistrationService.RegisterCommandsAsync(_client, guild);
    }
}