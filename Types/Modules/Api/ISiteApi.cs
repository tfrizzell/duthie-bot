using System.Text.RegularExpressions;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface ISiteApi
{
    IReadOnlySet<Guid> Supports { get; }

    public bool IsSupported(League leauge) => Supports.Contains(leauge.SiteId);

    public static DateTimeOffset ParseDateWithNoYear(string value, TimeZoneInfo? timezone = null)
    {
        timezone ??= TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var dateTime = DateTime.Parse(value);

        var present = new DateTimeOffset(dateTime, timezone.GetUtcOffset(dateTime));
        var dPresent = Math.Abs((DateTimeOffset.UtcNow - present).TotalMilliseconds);

        var past = present.AddYears(-1);
        var dPast = Math.Abs((DateTimeOffset.UtcNow - past).TotalMilliseconds);

        var future = present.AddYears(1);
        var dFuture = Math.Abs((DateTimeOffset.UtcNow - future).TotalMilliseconds);

        if (dFuture < dPast && dFuture < dPresent)
            return future;
        else if (dPast < dPresent)
            return past;
        else
            return present;
    }

    public static ulong ParseDollars(string value)
    {
        int scalar = 1;

        if (Regex.Match(value, @"\bM\b", RegexOptions.IgnoreCase).Success)
            scalar = 1000000;
        else if (Regex.Match(value, @"\bk\b", RegexOptions.IgnoreCase).Success)
            scalar = 1000;

        return (ulong)(scalar * decimal.Parse(Regex.Replace(value, @"[^\d.]+", "").Trim()));
    }
}