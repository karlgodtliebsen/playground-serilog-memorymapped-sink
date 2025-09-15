using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog.MemoryMapped.Queue.Configuration;
using Serilog.MemoryMapped.Queue.Serializers;

namespace Serilog.MemoryMapped.Sink.Configuration;

public static class MemoryMappedSinkConfigurator
{

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IMemoryMappedQueue, MemoryMappedQueue>();
        services.AddMemoryMappedQueueServices(configuration);
        return services;
    }

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, IOptions<MemoryMappedOptions> options)
    {
        services.AddMemoryMappedQueueServices(options);
        IFastSerializer serializer = new FastMemoryPackSerializer();
        services.TryAddSingleton<IMemoryMappedQueue>((sp) => new MemoryMappedQueue(options, serializer));
        return services;
    }
}