using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink.Console.Configuration;

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
        return serviceProvider.SetupSerilogWithSink(configuration, LogEventLevel.Information, action);
    }


    public static Serilog.ILogger SetupSerilogWithSink(this IServiceProvider serviceProvider, IConfiguration configuration, LogEventLevel level, Action<LoggerConfiguration>? action = null)
    {
        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        var logConfig = new LoggerConfiguration();
        logConfig = logConfig
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.With<TraceIdEnricher>()
            ;

        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, level);
        logConfig = logConfig.WriteTo.Sink(sink, level);
        action?.Invoke(logConfig);
        var logCfg = logConfig.ReadFrom.Configuration(configuration).CreateLogger();
        Log.Logger = logCfg;
        return logCfg;
    }
}