namespace Duthie.Types.Api;

public interface IApi
{
    IReadOnlySet<Guid> Supports { get; }

    public bool IsSupported(League leauge) => Supports.Contains(leauge.SiteId);
}