using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IWaiverApi : ISiteApi
{
    Task<IEnumerable<Waiver>?> GetWaiversAsync(League league);

    public string? GetWaiverUrl(League league, Waiver waiver) =>
        null;
}