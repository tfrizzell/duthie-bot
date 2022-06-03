using System.Runtime.CompilerServices;
using Duthie.Types.Common;
using Duthie.Types.Sites;

[assembly: InternalsVisibleTo("Duthie.Modules.MyVirtualGaming.Tests")]
namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingSiteProvider : ISiteProvider
{
    internal static readonly Guid SITE_ID = new Guid("40a06d17-e48f-49f1-9184-7393f035322c");

    public IReadOnlyCollection<Site> Sites
    {
        get => new Site[]
        {
            new Site
            {
                Id = SITE_ID,
                Name = "myvirtualgaming.com",
                Tags = new Tags { "psn", "ea nhl" },
                Enabled = true
            },
        };
    }
}