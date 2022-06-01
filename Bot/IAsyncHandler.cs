namespace Duthie.Bot;

public interface IAsyncHandler : IAsyncDisposable
{
    ValueTask RunAsync();
}