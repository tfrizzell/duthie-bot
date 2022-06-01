using Discord;
using Discord.WebSocket;
using Duthie.Bot.Configuration;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Commands;

public class PingCommand : BaseCommand
{
    private const int PONG_SIZE = 10;

    private readonly ILogger<PingCommand> _logger;
    private readonly AppInfo _appInfo;

    public PingCommand(
        ILogger<PingCommand> logger,
        AppInfo appInfo,
        DiscordConfiguration config) : base(config)
    {
        _logger = logger;
        _appInfo = appInfo;
    }

    protected override string Command { get => "ping"; }

    public override Task<SlashCommandOptionBuilder> BuildAsync() =>
        Task.FromResult(
            new SlashCommandOptionBuilder()
                .WithName(Command)
                .WithDescription($"Sends a ping to {_appInfo.Name} to make sure it's working.")
                .WithType(ApplicationCommandOptionType.SubCommand));

    protected override async Task HandleCommandAsync(SocketSlashCommand command)
    {
        await command.RespondAsync("Pong!");
    }
}