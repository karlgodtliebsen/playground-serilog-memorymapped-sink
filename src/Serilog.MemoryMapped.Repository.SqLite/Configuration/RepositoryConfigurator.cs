using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Sink.Forwarder.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

using SqLiteLogEventRepository = Serilog.MemoryMapped.Repository.SqLite.Repositories.SqLiteLogEventRepository;

namespace Serilog.MemoryMapped.Repository.SqLite.Configuration;

public static class RepositoryConfigurator
{

    public static IServiceCollection AddSqLiteServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(DatabaseConnectionOptions.SectionName + "_SqLite").Get<DatabaseConnectionOptions>();
        if (options is null) throw new ArgumentNullException(DatabaseConnectionOptions.SectionName);

        services.TryAddSingleton(Options.Create(options));
        services.TryAddSingleton<ILogEventRepository, SqLiteLogEventRepository>();
        return services;
    }
}