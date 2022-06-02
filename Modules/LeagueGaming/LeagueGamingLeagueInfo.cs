using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Duthie.Modules.LeagueGaming.Tests")]
namespace Duthie.Modules.LeagueGaming;

internal class LeagueGamingLeagueInfo
{
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }
    public int ForumId { get; set; }
}