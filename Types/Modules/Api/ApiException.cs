using System.Runtime.Serialization;

namespace Duthie.Types.Modules.Api;

public class ApiException : Exception
{
    public ApiException() : base() { }
    public ApiException(string? message) : base(message) { }

    public ApiException(string? message, Exception? innerException) : base(message, innerException) { }

    protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}