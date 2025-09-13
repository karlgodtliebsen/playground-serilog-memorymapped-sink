using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}