using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;
using Serilog.MemoryMapped.Sink.Sinks;

using System.Diagnostics;

using Xunit.Abstractions;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorker(ITestOutputHelper output)
{


    [Fact]
    public async Task VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {
        //TODO: add serilog config
        //$"Data Source={name};Journal Mode = WAL;";  


        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");
        services.AddForwarderServices(configuration);
        //services.AddSqLiteServices(configuration);

        services.AddLogging((loggingBuilder) =>
            {
                services.AddSerilog(loggingBuilder, configuration);
            }
        );

        var serviceProvider = services.BuildServiceProvider();
        var host = serviceProvider.BuildApplicationLoggingHost(configuration);

        await Task.Run(async () => await host.StartAsync(CancellationToken.None));

        SelfLog.Enable(msg => output.WriteLine($"Serilog: {msg}"));
        serviceProvider.SetupSerilog(configuration);

        Log.Logger.Verbose("the message template {UserId} {t1} {t2} {t3}", "the user", "the t1", "the t2", "the t3");
        await Log.CloseAndFlushAsync();
        await Task.Delay(10000);
    }
}


public static class SerilogConfigurator
{
    public static IHost BuildApplicationLoggingHost(this IServiceProvider serviceProvider, IConfiguration configuration)
    {

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole();
                });
                services.AddMemoryMappedServices("the name");
                //services.AddForwarderServices(configuration);
                services.AddSqLiteServices(configuration);

            });


        var host = builder.Build();
        host.Services.SetupSerilog(configuration);
        return host;
    }
    public static void AddSerilog(this IServiceCollection services, ILoggingBuilder loggingBuilder, IConfiguration configuration, Action<IServiceCollection, ILoggingBuilder, IConfiguration>? optionsAction = null)
    {
        services.AddSerilog();
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
        loggingBuilder.AddSerilog();
        optionsAction?.Invoke(services, loggingBuilder, configuration);
    }

    public static Serilog.ILogger SetupSerilog(this IServiceProvider serviceProvider, IConfiguration configuration, Action<LoggerConfiguration>? action = null)
    {
        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();
        var logConfig = new LoggerConfiguration();
        logConfig = logConfig
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.With<TraceIdEnricher>()
            ;

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, LogEventLevel.Verbose);
        logConfig
             .WriteTo.Sink(sink, LogEventLevel.Verbose)
             .MinimumLevel.Verbose()
             ;

        action?.Invoke(logConfig);

        Log.Logger = logConfig
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        return Log.Logger;
    }
}
public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));

            // Optionally add parent span ID
            if (activity.ParentSpanId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", activity.ParentSpanId.ToString()));
            }
        }
        else
        {
            // Fallback to generating a correlation ID if no Activity
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", Guid.NewGuid().ToString()));
        }
    }
}