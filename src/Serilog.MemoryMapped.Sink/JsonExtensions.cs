using Serilog.Events;

namespace Serilog.MemoryMapped.Sink;

public static class JsonExtensions
{

    public static string ToJson(this IReadOnlyDictionary<string, LogEventPropertyValue> property)
    {
        return System.Text.Json.JsonSerializer.Serialize(property);//TODO: fast serializer
    }
    public static string ToJson(this LogEvent logEvent)
    {
        return System.Text.Json.JsonSerializer.Serialize(logEvent);//TODO: fast serializer
    }
    public static string ToJson(this object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj);//TODO: fast serializer
    }
}