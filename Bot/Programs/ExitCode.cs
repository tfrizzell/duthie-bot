namespace Duthie.Bot;

internal static class ExitCode
{
    public const int Success = 0;
    public const int CommandRegistrationFailure = 1;
    public const int GuildUpdateFailure = 2;
    public const int UnhandledException = 4;
}