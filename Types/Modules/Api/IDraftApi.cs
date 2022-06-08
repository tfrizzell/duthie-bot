using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IDraftApi : ISiteApi
{
    Task<IEnumerable<DraftPick>?> GetDraftPicksAsync(League league);

    public string? GetDraftPickUrl(League league, DraftPick draftPick) =>
        null;
}