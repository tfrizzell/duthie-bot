using Duthie.Modules.MyVirtualGaming;
using Duthie.Types.Leagues;

namespace Duthie.Bot.Extensions;

public static class LeagueExtensions
{
    public static bool HasPluralTeamNames(this League league) =>
        league.Tags.Intersect(new string[] { "fifa", "esports", "tournament", "pickup", "club teams" }).Count() == 0;
}