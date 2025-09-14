using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Serilog.Debugging;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Sinks;
using Serilog.Parsing;

using System.Diagnostics;

using Xunit.Abstractions;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapper(ITestOutputHelper output)
{
    [Fact]
    public void VerifyMemoryMapperConfiguration()
    {

        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();
    }

    [Fact]
    public void VerifyMemoryMapperFormatters()
    {

        //var jsonFormatter = new JsonFormatter();
        //var renderedJsonFormatter = new JsonFormatter(renderMessage: true);
        //var originalCompactFormatter = new CompactJsonFormatter();
        //var originalRenderedJsonFormatter = new RenderedCompactJsonFormatter();
        var logEvent = CreateLogEvent();
        {
            using var textWriter = new StringWriter();
            var formatter = new JsonFormatter();
            formatter.Format(logEvent, textWriter);
            var renderedMessage = textWriter.ToString();
            output.WriteLine(renderedMessage);
        }
        output.WriteLine("-----------------------------------------");

        {
            using var textWriter = new StringWriter();
            var formatter = new JsonFormatter(renderMessage: true);
            formatter.Format(logEvent, textWriter);
            var renderedMessage = textWriter.ToString();
            output.WriteLine(renderedMessage);
        }
        output.WriteLine("-----------------------------------------");

        {
            using var textWriter = new StringWriter();
            var formatter = new CompactJsonFormatter();
            formatter.Format(logEvent, textWriter);
            var renderedMessage = textWriter.ToString();
            output.WriteLine(renderedMessage);
        }

        output.WriteLine("-----------------------------------------");

        {
            using var textWriter = new StringWriter();
            var formatter = new RenderedCompactJsonFormatter();
            formatter.Format(logEvent, textWriter);
            var renderedMessage = textWriter.ToString();
            output.WriteLine(renderedMessage);
        }
        output.WriteLine("-----------------------------------------");

        {
            var renderedMessage = logEvent.MessageTemplate.Render(logEvent.Properties, null);
            output.WriteLine(renderedMessage);
        }
        output.WriteLine("-----------------------------------------");

    }

    [Fact]
    public void VerifyTestSink()
    {
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink<TestLogEventSink>()
                .MinimumLevel.Verbose()
            .CreateLogger()
            ;

        Log.Logger.Verbose("the message template {UserId} {t1} {t2} {t3}", "the user", "the t1", "the t2", "the t3");
        Log.CloseAndFlush();
    }


    [Fact]
    public void VerifyLogSink()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();
        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(sink, LogEventLevel.Verbose)
                .WriteTo.Sink<TestLogEventSink>()
                .MinimumLevel.Verbose()
                .CreateLogger()
            ;

        Log.Logger.Verbose("the message template {UserId} {t1} {t2} {t3}", "the user", "the t1", "the t2", "the t3");
        Log.CloseAndFlush();
    }


    [Fact]
    public void VerifyMemoryMapperEnqueueAndDequeue()
    {
        //var jsonFormatter = new JsonFormatter();
        //var renderedJsonFormatter = new JsonFormatter(renderMessage: true);
        //var originalCompactFormatter = new CompactJsonFormatter();
        //var originalRenderedJsonFormatter = new RenderedCompactJsonFormatter();

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));

        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();

        var logEvent = CreateLogEvent();

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);
        sink.Emit(logEvent);

        var @mappedEvent = memoryMappedQueue.TryDequeue();
        @mappedEvent.Should().NotBeNull();
        output.WriteLine($"Message {@mappedEvent!.ToJson()}");
    }

    [Fact]
    public void VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));

        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.With<TraceIdEnricher>()
                .WriteTo.Sink(sink, LogEventLevel.Verbose)
                .MinimumLevel.Verbose()
                .CreateLogger()
            ;

        Log.Logger.Verbose("the message template {UserId} {t1} {t2} {t3}", "the user", "the t1", "the t2", "the t3");
        Log.CloseAndFlush();

        var @mappedEvent = memoryMappedQueue.TryDequeue();
        @mappedEvent.Should().NotBeNull();

        output.WriteLine($"Message {@mappedEvent!.ToJson()}");
    }

    [Fact]
    public void VerifyMultipleLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));

        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.With<TraceIdEnricher>()
                .WriteTo.Sink(sink, LogEventLevel.Verbose)
                .MinimumLevel.Verbose()
                .CreateLogger()
            ;

        for (int i = 0; i < 1000; i++)
        {
            Log.Logger.Information("the message template {UserId} {t1} {t2} {t3}", "the user", "the t1", "the t2", "the t3");
        }
        Log.CloseAndFlush();


    }

    private LogEvent CreateLogEvent()
    {

        var messageTemplateTokens = new List<MessageTemplateToken>
        {
            new PropertyToken( "UserId","RawText"),
            new TextToken( "Hello " ),
            new PropertyToken( "UserName","RawText"),
            new TextToken( "! " ),
        };

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        var logEventProperties = new List<LogEventProperty>()
        {
            new LogEventProperty("the name-1",new ScalarValue(42)),
            new LogEventProperty("the name-2",new ScalarValue(43))
        };
        var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug, new Exception("exception message"),
            new MessageTemplate("the message template {UserId} {t1} {t2} {t3} ", messageTemplateTokens), logEventProperties, traceId, spanId);
        return logEvent;
    }
}
//Log.Logger = new LoggerConfiguration()
//    .WriteTo.File(new CompactJsonFormatter(), "./logs/myapp.json")
//    .CreateLogger();


