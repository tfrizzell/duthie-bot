using Discord;
using Discord.WebSocket;

namespace Duthie.Bot.Commands;

public interface ICommand
{

    Task<SlashCommandOptionBuilder> BuildAsync();

    Task HandleAsync(SocketSlashCommand command);
}