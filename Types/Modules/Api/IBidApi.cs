using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IBidApi : ISiteApi
{
    Task<IEnumerable<Bid>?> GetBidsAsync(League league);

    public string? GetBidUrl(League league, Bid bid) =>
        null;
}