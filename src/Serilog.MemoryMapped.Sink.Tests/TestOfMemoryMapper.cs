using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog.Debugging;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.MemoryMapped.Serializers;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Sinks;
using Serilog.Parsing;


namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapper(ITestOutputHelper output)
{

    [Fact]
    public void VerifyMemoryMapperConfiguration()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));

        var name = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        output.WriteLine(name);
        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = name });
        services.AddMemoryMappedServices(options);
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
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        var name = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        output.WriteLine(name);
        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = name });
        services.AddMemoryMappedServices(options);

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();
        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);

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
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        var name = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        output.WriteLine(name);
        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = name });
        services.AddMemoryMappedServices(options);

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
    public void VerifyMemoryMapperEnqueueAndDequeueUsingTwoDifferentQueueBuffersWithJSon()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        var name = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        output.WriteLine(name);
        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = name });
        IFastSerializer serializer = new FastJsonSerializer();

        var memoryMappedQueue = new MemoryMappedQueue(options, serializer);

        var logEvent = CreateLogEvent();

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);
        sink.Emit(logEvent);
        serializer = new FastJsonSerializer();
        memoryMappedQueue = new MemoryMappedQueue(options, serializer);

        var @mappedEvent = memoryMappedQueue.TryDequeue();
        @mappedEvent.Should().NotBeNull();
        output.WriteLine($"Message {@mappedEvent!.ToJson()}");
    }

    [Fact]
    public void VerifyMemoryMapperEnqueueAndDequeueUsingTwoDifferentQueueBuffersWithMemoryPack()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        var name = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        output.WriteLine(name);
        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = name });
        IFastSerializer serializer = new FastMemoryPackSerializer();

        var memoryMappedQueue = new MemoryMappedQueue(options, serializer);

        var logEvent = CreateLogEvent();

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);
        sink.Emit(logEvent);
        serializer = new FastMemoryPackSerializer();
        memoryMappedQueue = new MemoryMappedQueue(options, serializer);

        var @mappedEvent = memoryMappedQueue.TryDequeue();
        @mappedEvent.Should().NotBeNull();
        output.WriteLine($"Message {@mappedEvent!.ToJson()}");
    }


    [Fact]
    public void VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {

        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));

        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = "the name" });
        services.AddMemoryMappedServices(options);

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

        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));

        IServiceCollection services = new ServiceCollection();
        var options = Options.Create(new MemoryMappedOptions() { Name = "the name" });
        services.AddMemoryMappedServices(options);

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

        for (var i = 0; i < 1000; i++)
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
            new("the name-1", new ScalarValue(42)),
            new("the name-2", new ScalarValue(43))
        };
        var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug, new Exception("exception message"),
            new MessageTemplate("the message template {UserId} {t1} {t2} {t3} ", messageTemplateTokens), logEventProperties, traceId, spanId);
        return logEvent;
    }
}


