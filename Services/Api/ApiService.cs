using System.Reflection;
using Duthie.Types.Api;
using Duthie.Types.Leagues;
using Duthie.Types.Sites;

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

    public IApi? Get(League league) =>
        Get(league.SiteId);

    public T? Get<T>(League league) where T : class, IApi =>
        Get<T>(league.SiteId);

    public IApi? Get(Site site) =>
        Get(site.Id);

    public T? Get<T>(Site site) where T : class, IApi =>
        Get<T>(site.Id);

    public IApi? Get(Guid siteId) =>
        Get<ISiteApi>(siteId);

    public T? Get<T>(Guid siteId) where T : class, IApi
    {
        if (!Apis.ContainsKey(siteId) || !Apis[siteId].ContainsKey(typeof(T)))
            return null;

        return (T)Apis[siteId][typeof(T)];
    }

    public ApiService Register(params IApi[] apis)
    {
        foreach (var api in apis)
        {
            foreach (var type in ApiTypes)
            {
                if (!type.IsAssignableFrom(api.GetType()))
                    continue;

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

        return this;
    }
}