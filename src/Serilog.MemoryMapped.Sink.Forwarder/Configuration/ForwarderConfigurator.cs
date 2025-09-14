using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Serilog.MemoryMapped.Sink.Forwarder.WorkerServices;

namespace Serilog.MemoryMapped.Sink.Forwarder.Configuration;

public static class ForwarderConfigurator
{

    public static IServiceCollection AddForwarderServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddTransient<ILogEventMemoryMappedShippingClient, LogEventMemoryMappedShippingClient>();
        services.TryAddTransient<ILogEventForwarder, LogEventForwarder>();
        return services;
    }
    public static IServiceCollection AddHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<LogEventShippingServiceHost>();
        services.AddHostedService<LogEventShippingServiceHost>();
        return services;
    }
}