using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Serilog.Configuration;
using Serilog.Events;
using Serilog.MemoryMapped.Sink.Sinks;

namespace Serilog.MemoryMapped.Sink.Configuration;

public static class MemoryMappedSinkConfigurator
{

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, IConfiguration configuration, string name)
    {
        //TODO: use configuration to add a name using IOptions instead
        services.AddMemoryMappedServices(name);
        return services;
    }

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, string name)
    {
        services.TryAddSingleton<IMemoryMappedQueue>((sp) => new MemoryMappedQueue(name));//TODO: Can be made to use ServiceProvider of some IOptions based config
        return services;
    }

    public static LoggerConfiguration SetupApplicationLoggingMsSqlSink(this LoggerSinkConfiguration sinkConfiguration,
        IServiceProvider serviceProvider, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
    {
        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        var sink = new LogEventMemoryMappedSink(memoryMappedQueue, restrictedToMinimumLevel);
        return sinkConfiguration.Sink(sink, restrictedToMinimumLevel);
    }
}
