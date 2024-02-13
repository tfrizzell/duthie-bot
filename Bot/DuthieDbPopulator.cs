using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types.Leagues;
using Duthie.Types.Sites;
using Microsoft.EntityFrameworkCore;

namespace Duthie.Bot;

internal static class DuthieDbPopulator
{
    public static async Task PopulateAsync(this DuthieDbContext context)
    {
        await PopulateSitesAsync(context);
        await PopulateLeaguesAsync(context);
    }

    private static async Task PopulateSitesAsync(DuthieDbContext context)
    {
        var sites = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(ISiteProvider).IsAssignableFrom(t))
            .SelectMany(t => ((ISiteProvider)Activator.CreateInstance(t)!).Sites);

        foreach (var site in sites)
        {
            if (!await context.Set<Site>().AnyAsync(s => s.Id == site.Id))
                await context.Set<Site>().AddAsync(site);
        }

        await context.SaveChangesAsync();
    }

    private static async Task PopulateLeaguesAsync(DuthieDbContext context)
    {
        var leagues = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(ILeagueProvider).IsAssignableFrom(t))
            .SelectMany(t => ((ILeagueProvider)Activator.CreateInstance(t)!).Leagues);

        var affiliates = new List<LeagueAffiliate>();

        foreach (var league in leagues)
        {
            context.Attach(league.Site);

            if (!await context.Set<League>().AnyAsync(l => l.Id == league.Id))
            {
                affiliates.AddRange(league.Affiliates ?? new List<LeagueAffiliate>());
                league.Affiliates = null;
                await context.Set<League>().AddAsync(league);
            }
        }

        await context.Set<LeagueAffiliate>().RemoveRangeAsync(await context.Set<LeagueAffiliate>().ToListAsync());
        await context.SaveChangesAsync();

        await context.Set<LeagueAffiliate>().AddRangeAsync(affiliates);
        await context.SaveChangesAsync();
    }
}