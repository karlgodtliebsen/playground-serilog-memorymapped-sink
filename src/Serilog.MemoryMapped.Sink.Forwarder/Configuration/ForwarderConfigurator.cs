using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Sink.Forwarder.Repositories;
using Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

namespace Serilog.MemoryMapped.Sink.Forwarder.Configuration;

public static class ForwarderConfigurator
{

    public static IServiceCollection AddForwarderServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.TryAddSingleton<ILogEventMemoryMappedShippingClient, LogEventMemoryMappedShippingClient>();

        services.TryAddSingleton<LogEventShippingHostedService>();
        services.AddHostedService<LogEventShippingHostedService>();


        return services;
    }
    public static IServiceCollection AddHostServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.TryAddSingleton<ILogEventMemoryMappedShippingClient, LogEventMemoryMappedShippingClient>();
        services.TryAddSingleton<LogEventShippingHostedService>();
        services.AddHostedService<LogEventShippingHostedService>();
        return services;
    }

    public static IServiceCollection AddSqLiteServices(this IServiceCollection services, IConfiguration configuration)
    {

        DatabaseConnectionOptions options = new DatabaseConnectionOptions();
        options.ConnectionString = "Data Source=/var/logs/logging.db";

        services.TryAddSingleton(Options.Create(options));
        services.TryAddSingleton<ILogEventForwarder, LogEventForwarder>();
        services.TryAddSingleton<ILogEventRepository, SqLiteLogEventRepository>();

        return services;
    }
}