using Serilog.Events;
using Serilog.Parsing;

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Serilog.MemoryMapped.Sink;

// Using attributes directly on the abstract class
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(WrapperPropertyToken), "property")]
[JsonDerivedType(typeof(WrapperTextToken), "text")]
public abstract class WrapperMessageTemplateToken
{
    // Your base class properties
}

public class WrapperPropertyToken : WrapperMessageTemplateToken
{
    public string PropertyName { get; set; } = null!;
    public string? Format { get; set; }
    public string? RawText { get; set; }
}

public class WrapperTextToken : WrapperMessageTemplateToken
{
    public string Text { get; set; } = null!;
}


// Using attributes directly on the abstract class
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(WrapperPropertyToken), "property")]
[JsonDerivedType(typeof(WrapperTextToken), "text")]
public abstract class WrapperLogEventPropertyValue
{
    // Your base class properties
}



public class LogEventWrapper
{
    public LogEventWrapper()
    {
    }
    public LogEventWrapper(LogEvent @event)
    {
        IEnumerable<WrapperMessageTemplateToken?> tokens = @event.MessageTemplate.Tokens.Select<MessageTemplateToken, WrapperMessageTemplateToken?>(pt =>
        {
            if (pt is PropertyToken propertyToken)
            {
                return new WrapperPropertyToken() { PropertyName = propertyToken.PropertyName, RawText = propertyToken.ToString(), Format = propertyToken.Format };
            }
            if (pt is TextToken token)
            {
                return new WrapperTextToken() { Text = token.Text };
            }

            return null;
        });

        this.Timestamp = @event.Timestamp;
        this.Level = @event.Level;
        this.TraceId = @event.TraceId;
        this.SpanId = @event.SpanId;
        this.MessageTemplateTokens = tokens.Where(t => t is not null).ToList();
        this.MessageTemplateText = @event.MessageTemplate.Text;
        this.Exception = @event.Exception;
        this.Properties = [];//new List<LogEventProperty>(@event.Properties.Select(x => new LogEventProperty(x.Key, x.Value)));
    }


    public LogEvent CreateLogEvent()
    {
        if (this.TraceId is not null && this.SpanId is not null)
        {
            return new LogEvent(this.Timestamp, this.Level, this.Exception, GetMessageTemplate(), this.Properties, this.TraceId.Value, this.SpanId.Value);
        }
        var @event = new LogEvent(this.Timestamp, this.Level, this.Exception, GetMessageTemplate(), this.Properties);
        return @event;
    }

    /// <summary>
    /// The time at which the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// The level of the event.
    /// </summary>
    public LogEventLevel Level { get; set; }

    /// <summary>
    /// The id of the trace that was active when the event was created, if any.
    /// </summary>
    public ActivityTraceId? TraceId { get; set; }

    /// <summary>
    /// The id of the span that was active when the event was created, if any.
    /// </summary>
    public ActivitySpanId? SpanId { get; set; }

    /// <summary>
    /// The message template describing the event.
    /// </summary>
    public string MessageTemplateText { get; init; } = null!;

    public List<WrapperMessageTemplateToken?> MessageTemplateTokens { get; set; } = [];

    public MessageTemplate GetMessageTemplate()
    {
        //if (MessageTemplateText is null || MessageTemplateTokens = []) return new MessageTemplate();
        IEnumerable<MessageTemplateToken?> tokens = MessageTemplateTokens!.Select<WrapperMessageTemplateToken, MessageTemplateToken>(pt =>
        {
            if (pt is WrapperPropertyToken propertyToken)
            {
                return new PropertyToken(propertyToken.PropertyName, propertyToken.RawText!, propertyToken.Format);
            }
            if (pt is WrapperTextToken token)
            {
                return new TextToken(token.Text);
            }
            return null!;
        });

        return new MessageTemplate(this.MessageTemplateText, tokens.Where(t => t is not null).ToList()!);
    }


    /// <summary>
    /// Properties associated with the event, including those presented in <see cref="LogEvent.MessageTemplate"/>.
    /// </summary>
    public IList<LogEventProperty> Properties { get; init; } = [];
    // ScalarValue : LogEventPropertyValue
    //DictionaryValue
    //SequenceValue
    //StructureValue

    /// <summary>
    /// An exception associated with the event, or null.
    /// </summary>
    public Exception? Exception { get; set; }
}