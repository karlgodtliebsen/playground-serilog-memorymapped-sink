using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink.Tests;

public static class SerilogConfigurator
{
    public static void AddSerilog(this IServiceCollection services, ILoggingBuilder loggingBuilder, IConfiguration configuration, Action<IServiceCollection, ILoggingBuilder, IConfiguration>? optionsAction = null)
    {
        services.AddSerilog();
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
        loggingBuilder.AddSerilog();
        optionsAction?.Invoke(services, loggingBuilder, configuration);
    }

    public static Serilog.ILogger SetupSerilogWithSink(this IServiceProvider serviceProvider, IConfiguration configuration, Action<LoggerConfiguration>? action = null)
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

        Log.Logger = logConfig.ReadFrom.Configuration(configuration).CreateLogger();

        return Log.Logger;
    }
}