using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Serilog.MemoryMapped.Sink.Configuration;

public static class MemoryMappedSinkConfigurator
{

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.GetSection(MemoryMappedOptions.SectionName).Get<MemoryMappedOptions>();
        if (cfg is null) throw new ArgumentNullException(MemoryMappedOptions.SectionName);

        var options = Options.Create(cfg);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IMemoryMappedQueue, MemoryMappedQueue>();
        return services;
    }

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, IOptions<MemoryMappedOptions> options)
    {
        services.TryAddSingleton<IMemoryMappedQueue>((sp) => new MemoryMappedQueue(options));
        return services;
    }
}