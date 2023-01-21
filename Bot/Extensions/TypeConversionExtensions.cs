using Discord.WebSocket;
using Duthie.Types.Guilds;

namespace Duthie.Bot.Extensions;

public static class SocketGuildToGuildConverter
{
    public static Guild ToGuild(this SocketGuild guild) =>
        new Guild
        {
            Id = guild.Id,
            Name = guild.Name,
            DefaultChannelId = guild.GetDefaultTextChannelId() ?? 0,
        };
}