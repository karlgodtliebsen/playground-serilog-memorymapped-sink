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

    public static Serilog.ILogger SetupSerilogWithSink(this IServiceProvider serviceProvider, LogEventLevel level = LogEventLevel.Information)
    {
        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logCfg = CreateMemoryMappedLogger(memoryMappedQueue, configuration, level);
        Log.Logger = logCfg;
        return logCfg;
    }

    public static ILogger CreateMemoryMappedLogger(IMemoryMappedQueue memoryMappedQueue, IConfiguration configuration, LogEventLevel level = LogEventLevel.Information)
    {
        var logConfig = new LoggerConfiguration()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.With<TraceIdEnricher>()
            .WriteTo.Sink(new LogEventMemoryMappedSink(memoryMappedQueue, level), level)
            .ReadFrom.Configuration(configuration);

        return logConfig.CreateLogger();
    }

    public static ILogger CreateConsumerLogger(IConfiguration configuration, string? context = null)
    {
        var config = new LoggerConfiguration()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext();

        if (!string.IsNullOrEmpty(context))
        {
            config = config.Enrich.WithProperty("TechnicalContext", context);
        }

        return config.ReadFrom.Configuration(configuration)
            .CreateLogger();
    }

    public static ILogger CreateMonitoringLogger(IConfiguration configuration, string? context = null)
    {
        var config = new LoggerConfiguration()
            .Enrich.WithProperty("LogCategory", "Monitoring");
        if (!string.IsNullOrEmpty(context))
        {
            config = config.Enrich.WithProperty("TechnicalContext", context);
        }

        return config
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}

