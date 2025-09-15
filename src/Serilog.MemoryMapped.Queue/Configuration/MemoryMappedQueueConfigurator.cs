using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog.MemoryMapped.Queue.Monitor;
using Serilog.MemoryMapped.Queue.Serializers;
using Serilog.MemoryMapped.Sink.Configuration;

namespace Serilog.MemoryMapped.Queue.Configuration;

public static class MemoryMappedQueueConfigurator
{

    public static IServiceCollection AddMemoryMappedQueueServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.GetSection(MemoryMappedOptions.SectionName).Get<MemoryMappedOptions>();
        if (cfg is null) throw new ArgumentNullException(MemoryMappedOptions.SectionName);
        var options = Options.Create(cfg);
        return services.AddMemoryMappedQueueServices(options);
    }

    public static IServiceCollection AddMemoryMappedQueueServices(this IServiceCollection services, IOptions<MemoryMappedOptions> mmOptions)
    {
        if (mmOptions is null) throw new ArgumentNullException(MemoryMappedOptions.SectionName);
        services.TryAddSingleton(mmOptions);

        var monOptions = Options.Create(new MonitoringOptions());
        services.TryAddSingleton(monOptions);

        services.TryAddTransient<IFastSerializer, FastMemoryPackSerializer>();
        services.TryAddTransient<IMemoryMappedQueueMonitor, MemoryMappedQueueMonitor>();
        return services;
    }
}