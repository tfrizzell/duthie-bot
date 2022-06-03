namespace Duthie.Types.Sites;

public interface ISiteProvider
{
    IReadOnlyCollection<Site> Sites { get; }
}