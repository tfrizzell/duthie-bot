using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IApi
{
    IReadOnlySet<Guid> Supports { get; }

    public bool IsSupported(League leauge) => Supports.Contains(leauge.SiteId);

    public static DateTimeOffset ParseDateWithNoYear(string value, TimeZoneInfo? timezone = null)
    {
        timezone ??= TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var parsed = DateTime.Parse(value);

        var present = new DateTimeOffset(parsed, timezone.GetUtcOffset(parsed));
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
}