namespace Duthie.Bot.Configuration;

public class DiscordConfiguration
{
    public string Token { get; set; } = "";
    public bool AcceptCommandsFromBots { get; set; } = false;
    public IEnumerable<ulong> Developers { get; set; } = new List<ulong>();
}