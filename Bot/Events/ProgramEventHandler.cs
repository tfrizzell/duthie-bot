using Discord.WebSocket;
using Duthie.Bot.Extensions;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Events;

public class ProgramEventHandler : IAsyncHandler
{
    private readonly ILogger<ProgramEventHandler> _logger;
    private readonly DiscordShardedClient _client;
    private readonly AppInfo _appInfo;

    public ProgramEventHandler(
        ILogger<ProgramEventHandler> logger,
        DiscordShardedClient client,
        AppInfo appInfo)
    {
        _logger = logger;
        _client = client;
        _appInfo = appInfo;
    }

    public ValueTask RunAsync()
    {
        _logger.LogInformation($"Starting {_appInfo.Name} v{_appInfo.Version}");

        AppDomain.CurrentDomain.DomainUnload += HandleShutdown;
        AppDomain.CurrentDomain.ProcessExit += HandleShutdown;
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        System.Console.CancelKeyPress += HandleShutdown;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        System.Console.CancelKeyPress -= HandleShutdown;
        AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
        AppDomain.CurrentDomain.ProcessExit -= HandleShutdown;
        AppDomain.CurrentDomain.DomainUnload -= HandleShutdown;
        return ValueTask.CompletedTask;
    }

    private void HandleShutdown(object? sender, EventArgs args)
    {
        _logger.LogInformation($"Shutting down {_appInfo.Name} v{_appInfo.Version}");

        Task.Run(async () =>
        {
            if (_client != null)
            {
                try
                {
                    await _client.ShowStoppingStatusAsync();
                    await _client.LogoutAsync();
                    await _client.StopAsync();
                }
                catch (Exception) { }
            }
        }).GetAwaiter().GetResult();
    }

    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        _logger.LogError((Exception?)args.ExceptionObject, "");

        Task.Run(async () =>
        {
            if (_client != null)
            {
                try
                {
                    await _client.ShowErrorStatusAsync();
                    await _client.LogoutAsync();
                    await _client.StopAsync();
                }
                catch (Exception) { }
            }
        }).GetAwaiter().GetResult();

        Environment.Exit(ExitCode.UnhandledException);
    }
}