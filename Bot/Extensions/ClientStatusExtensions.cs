using Discord;
using Discord.WebSocket;

namespace Duthie.Bot.Extensions;

public static class ClientStatusExtensions
{
    public static async Task ShowStartingStatusAsync(this BaseSocketClient client, string message = "Starting...", UserStatus status = UserStatus.DoNotDisturb, ActivityType type = ActivityType.Playing) =>
        await client.SetStatusAsync(message, status, type);

    public static async Task ShowRegisteringStatusAsync(this BaseSocketClient client, string message = "Registering commands...", UserStatus status = UserStatus.DoNotDisturb, ActivityType type = ActivityType.Streaming) =>
        await client.SetStatusAsync(message, status, type);

    public static async Task ShowOnlineStatusAsync(this BaseSocketClient client, string message = "/duthie", UserStatus status = UserStatus.Online, ActivityType type = ActivityType.Watching) =>
        await client.SetStatusAsync(message, status, type);

    public static async Task ShowDisconnectedStatusAsync(this BaseSocketClient client, string message = "DISCONNECTED", UserStatus status = UserStatus.Idle, ActivityType type = ActivityType.Playing) =>
        await client.SetStatusAsync(message, status, type);

    public static async Task ShowErrorStatusAsync(this BaseSocketClient client, string message = "ERROR", UserStatus status = UserStatus.DoNotDisturb, ActivityType type = ActivityType.Listening) =>
        await client.SetStatusAsync(message, status, type);

    public static async Task ShowStoppingStatusAsync(this BaseSocketClient client, string message = "Shutting down...", UserStatus status = UserStatus.DoNotDisturb, ActivityType type = ActivityType.Playing) =>
        await client.SetStatusAsync(message, status, type);

    private static async Task SetStatusAsync(this BaseSocketClient client, string message, UserStatus status, ActivityType type)
    {
        await client.SetGameAsync(message, null, type);
        await client.SetStatusAsync(status);
    }
}