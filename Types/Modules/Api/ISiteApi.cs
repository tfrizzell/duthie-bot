using System.Text.RegularExpressions;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface ISiteApi
{
    IReadOnlySet<Guid> Supports { get; }

    public bool IsSupported(League leauge) => Supports.Contains(leauge.SiteId);

    protected static readonly TimeZoneInfo DefaultTimezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    protected static DateTimeOffset ParseDateTime(string value, TimeZoneInfo? timezone = null)
    {
        var dateTime = DateTime.Parse(value.Trim());
        return new DateTimeOffset(dateTime, (timezone ?? DefaultTimezone).GetUtcOffset(dateTime));
    }

    protected static DateTimeOffset ParseDateWithNoYear(string value, TimeZoneInfo? timezone = null)
    {
        var present = ParseDateTime(value, timezone);
        var dPresent = Math.Abs((DateTimeOffset.UtcNow - present).TotalMilliseconds);

        var past = ParseDateTime(present.AddYears(-1).ToString("yyyy-MM-dd"), timezone);
        var dPast = Math.Abs((DateTimeOffset.UtcNow - past).TotalMilliseconds);

        var future = ParseDateTime(present.AddYears(1).ToString("yyyy-MM-dd"), timezone);
        var dFuture = Math.Abs((DateTimeOffset.UtcNow - future).TotalMilliseconds);

        if (dFuture < dPast && dFuture < dPresent)
            return future;
        else if (dPast < dPresent)
            return past;
        else
            return present;
    }

    protected static ulong ParseDollars(string value)
    {
        int scalar = 1;

        if (Regex.Match(value, @"\bM\b", RegexOptions.IgnoreCase).Success)
            scalar = 1000000;
        else if (Regex.Match(value, @"\bk\b", RegexOptions.IgnoreCase).Success)
            scalar = 1000;

        return (ulong)(scalar * decimal.Parse(Regex.Replace(value, @"[^\d.]+", "").Trim()));
    }
}