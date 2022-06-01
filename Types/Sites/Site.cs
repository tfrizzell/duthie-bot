namespace Duthie.Types;

public class Site
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public IReadOnlyCollection<string> Tags { get; set; }
    public bool Enabled { get; set; } = true;

    public virtual IReadOnlyCollection<League> Leagues { get; set; }
}