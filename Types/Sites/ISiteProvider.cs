namespace Duthie.Types;

public interface ISiteProvider
{
    IReadOnlyCollection<Site> Sites { get; }
}