using System.Reflection;
using Duthie.Types;
using Duthie.Types.Api;

namespace Duthie.Bot.Services;

public class ApiService
{
    private static readonly IReadOnlyCollection<Type> ApiTypes = new Type[]
    {
        typeof(ISiteApi),
        typeof(IDailyStarsApi),
        typeof(IGamesApi),
        typeof(ILeagueInfoApi),
        typeof(INewsApi),
        typeof(ITeamsApi),
    };

    private readonly IDictionary<Guid, Dictionary<Type, IApi>> Apis = new Dictionary<Guid, Dictionary<Type, IApi>>();

    public IApi Get(League league) =>
        Get(league.SiteId);

    public T Get<T>(League league) where T : class, IApi =>
        Get<T>(league.SiteId);

    public IApi Get(Site site) =>
        Get(site.Id);

    public T Get<T>(Site site) where T : class, IApi =>
        Get<T>(site.Id);

    public IApi Get(Guid siteId) =>
        Get<ISiteApi>(siteId);

    public T Get<T>(Guid siteId) where T : class, IApi
    {
        if (!Apis.ContainsKey(siteId))
            throw new TargetException($"No APIs found for site {siteId}");

        if (!Apis[siteId].ContainsKey(typeof(T)))
            throw new TargetException($"No {typeof(T).Name} for site {siteId}");

        return (T)Apis[siteId][typeof(T)];
    }

    public void Register(params IApi[] apis)
    {
        foreach (var api in apis)
        {
            foreach (var type in ApiTypes)
            {
                foreach (var siteId in api.Supports)
                {
                    if (Apis.ContainsKey(siteId) && Apis[siteId].ContainsKey(type) && Apis[siteId][type] != api)
                        throw new TargetException($"Conflicting instances of {type.Name} for site {siteId}");

                    if (!Apis.ContainsKey(siteId))
                        Apis.Add(siteId, new Dictionary<Type, IApi>());

                    Apis[siteId].Add(type, api);
                }
            }
        }
    }
}