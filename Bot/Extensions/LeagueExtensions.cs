using Duthie.Modules.MyVirtualGaming;
using Duthie.Types.Leagues;

namespace Duthie.Bot.Extensions;

public static class LeagueExtensions
{

    public static string[]? GetAffiliateIds(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("AffiliatedLeagueIds");
        return prop?.GetValue(league.Info) as string[];
    }

    public static string? GetLeagueId(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("LeagueId");
        return prop?.GetValue(league.Info) as string;
    }

    public static bool HasPluralTeamNames(this League league) =>
        league.Tags.Intersect(new string[] { "fifa", "esports", "tournament", "pickup", "club teams" }).Count() == 0;

    public static bool HasAffiliates(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("AffiliatedLeagueIds");
        return prop != null && (prop.GetValue(league.Info) as string[])?.Count() > 0;
    }
}