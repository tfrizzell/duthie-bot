using Duthie.Types.Api.Data;
using Duthie.Types.Guilds;
using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IGameApi : ISiteApi
{
    Task<IEnumerable<Game>?> GetGamesAsync(League league);

    /// <summary>
    /// Generates embed data to attach to the guild message.
    /// </summary>
    /// <param name="message">The message being sent.</param>
    /// <param name="game">The game the message is being generated for.</param>
    /// <param name="league">The league the message is being generator for.</param>
    /// <returns>
    /// A tuple containing a boolean that indicates whether the updated message to be send,
    /// and the <see cref="GuildMessageEmbed" /> to be attached to the message.
    /// </returns>
    public (string, GuildMessageEmbed?) GetMessageEmbed(string message, Game game, League league) =>
        (message, null);
}