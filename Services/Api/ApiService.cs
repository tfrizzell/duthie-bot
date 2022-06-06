using System.Reflection;
using Duthie.Types.Api;
using Duthie.Types.Leagues;
using Duthie.Types.Sites;

namespace Duthie.Services.Api;

public class ApiService
{
    private static readonly IReadOnlyCollection<Type> ApiTypes = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetName().Name == "Duthie.Types")
        .SelectMany(a => a.GetTypes())
        .Where(t => t.IsAbstract && t.FullName?.StartsWith("Duthie.Types.Api") == true && t.FullName?.StartsWith("Duthie.Types.Api.Data") == false)
        .ToList();

    private readonly IDictionary<Guid, Dictionary<Type, ISiteApi>> Apis = new Dictionary<Guid, Dictionary<Type, ISiteApi>>();

    public ISiteApi? Get(League league) =>
        Get(league.SiteId);

    public T? Get<T>(League league) where T : class, ISiteApi =>
        Get<T>(league.SiteId);

    public ISiteApi? Get(Site site) =>
        Get(site.Id);

    public T? Get<T>(Site site) where T : class, ISiteApi =>
        Get<T>(site.Id);

    public ISiteApi? Get(Guid siteId) =>
        Get<ISiteApi>(siteId);

    public T? Get<T>(Guid siteId) where T : class, ISiteApi
    {
        if (!Apis.ContainsKey(siteId) || !Apis[siteId].ContainsKey(typeof(T)))
            return null;

        return (T)Apis[siteId][typeof(T)];
    }

    public ApiService Register(params ISiteApi[] apis)
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
                        Apis.Add(siteId, new Dictionary<Type, ISiteApi>());

                    Apis[siteId].Add(type, api);
                }
            }
        }

        return this;
    }
}