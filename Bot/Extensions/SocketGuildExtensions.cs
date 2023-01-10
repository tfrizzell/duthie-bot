using Discord.WebSocket;

namespace Duthie.Bot.Extensions;

public static class GuildUtils
{
    public static ulong GetDefaultTextChannelId(this SocketGuild guild) =>
        (guild.TextChannels.FirstOrDefault(c => !(c is SocketVoiceChannel)) ?? guild.SystemChannel).Id;
}