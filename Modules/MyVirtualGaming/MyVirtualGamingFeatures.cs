namespace Duthie.Modules.MyVirtualGaming;

[Flags]
internal enum MyVirtualGamingFeatures
{
    None = 0,
    RecentTransactions = 1,
    DraftCentre = 2,
    All = RecentTransactions | DraftCentre,
}