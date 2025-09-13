using Serilog.Events;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;

public static class RepositoryExtensions
{
    public static string? ToJson(this Exception? exception)
    {
        if (exception is null) return null;
        //exception = TruncateIfNeeded(entry.Exception, forwarderOptions.MaxExceptionLength),

        return System.Text.Json.JsonSerializer.Serialize(exception);//TODO: fast serializer
    }
    public static string ToJson(this IReadOnlyDictionary<string, LogEventPropertyValue> property)
    {
        return System.Text.Json.JsonSerializer.Serialize(property);//TODO: fast serializer
    }

}