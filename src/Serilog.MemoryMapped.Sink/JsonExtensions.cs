using Serilog.Events;
using Serilog.Parsing;

using System.Text.Json;

using System.Text.Json.Serialization.Metadata;

namespace Serilog.MemoryMapped.Sink;

public static class JsonExtensions
{

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Dictionary<string, object> GetPropertiesAsObjects(this LogEvent logEvent)
    {
        return LogEventPropertiesSerializer.ConvertPropertiesToObjects(logEvent.Properties);
    }

    public static string ToJson(this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        var convertedProperties = LogEventPropertiesSerializer.ConvertPropertiesToObjects(properties);
        return JsonSerializer.Serialize(convertedProperties, Options);
    }

    public static string ToJson(this object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj);//TODO: fast serializer
    }

    private static JsonSerializerOptions AddToOptions()
    {
        // Configure the type resolver for polymorphic serialization
        Options.TypeInfoResolver = JsonTypeInfoResolver.Combine(
            new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ConfigurePolymorphism }
            }
        );
        return Options;
    }

    private static void ConfigurePolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(MessageTemplateToken))
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type",
                DerivedTypes =
                {
                    new JsonDerivedType(typeof(PropertyToken), "property"),
                    new JsonDerivedType(typeof(TextToken), "text")
                }
            };
        }
    }
}
public static class LogEventPropertiesSerializer
{
    public static Dictionary<string, object> ConvertPropertiesToObjects(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in properties)
        {
            result[kvp.Key] = ConvertPropertyValue(kvp.Value);
        }

        return result;
    }

    private static object ConvertPropertyValue(LogEventPropertyValue propertyValue)
    {
        return propertyValue switch
        {
            ScalarValue sv => ConvertScalarValue(sv),
            SequenceValue seq => seq.Elements.Select(ConvertPropertyValue).ToArray(),
            StructureValue structure => ConvertStructureValue(structure),
            DictionaryValue dict => ConvertDictionaryValue(dict),
            _ => propertyValue.ToString()
        };
    }

    private static object ConvertScalarValue(ScalarValue scalar)
    {
        if (scalar.Value is null) return string.Empty;
        return scalar.Value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b,
            byte or sbyte or short or ushort or int or uint or long or ulong => scalar.Value,
            float f => f,
            double d => d,
            decimal m => m,
            DateTime dt => dt,
            DateTimeOffset dto => dto,
            TimeSpan ts => ts,
            Guid g => g,
            _ => scalar.Value!.ToString() ?? string.Empty
        };
    }

    private static Dictionary<string, object> ConvertStructureValue(StructureValue structure)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(structure.TypeTag))
        {
            result["$type"] = structure.TypeTag;
        }

        foreach (var property in structure.Properties)
        {
            result[property.Name] = ConvertPropertyValue(property.Value);
        }

        return result;
    }

    private static Dictionary<string, object> ConvertDictionaryValue(DictionaryValue dictionary)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in dictionary.Elements)
        {
            var key = ConvertPropertyValue(kvp.Key)?.ToString() ?? "null";
            result[key] = ConvertPropertyValue(kvp.Value);
        }

        return result;
    }
}
