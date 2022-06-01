namespace Duthie.Types.Api;

// public interface ISiteApi : IDailyStarsApi, IGamesApi, ILeagueInfoApi, INewsApi, ITeamsApi
public interface ISiteApi : IApi, ILeagueInfoApi
{
    // new IEnumerable<Guid> Supports();
    // public new bool Supports(League leauge) => Supports().Contains(leauge.SiteId);
}