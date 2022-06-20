using Duthie.Modules.MyVirtualGaming;
using Duthie.Types.Leagues;

namespace Duthie.Bot.Extensions;

public static class LeagueExtensions
{

    public static string?[]? GetAffiliateIds(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("AffiliatedLeagueIds");
        return (prop?.GetValue(league.Info) as object[])?.Select(l => l?.ToString()).ToArray();
    }

    public static T?[]? GetAffiliateIds<T>(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("AffiliatedLeagueIds");
        return (prop?.GetValue(league.Info) as object[])?.Cast<T>().ToArray();
    }

    public static string? GetLeagueId(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("LeagueId");
        return prop?.GetValue(league.Info)?.ToString();
    }

    public static T? GetLeagueId<T>(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("LeagueId");
        return (T?)prop?.GetValue(league.Info);
    }

    public static string GetPlatform(this League league)
    {
        return league.Tags.Contains("xbox") ? "xbox" : "psn";
    }

    public static bool HasPluralTeamNames(this League league) =>
        league.Tags.Intersect(new string[] { "fifa", "esports", "tournament", "pickup", "club teams" }).Count() == 0;

    public static bool HasAffiliates(this League league)
    {
        var prop = league.Info?.GetType().GetProperty("AffiliatedLeagueIds");
        return prop != null && (prop.GetValue(league.Info) as string[])?.Count() > 0;
    }
}