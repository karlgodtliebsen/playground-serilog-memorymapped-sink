using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Repository.MsSql.Repositories;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

namespace Serilog.MemoryMapped.Repository.MsSql.Configuration;

public static class RepositoryConfigurator
{
    public static IServiceCollection AddMsSqlServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(DatabaseConnectionOptions.SectionName + "_MsSql").Get<DatabaseConnectionOptions>();
        if (options is null) throw new ArgumentNullException(DatabaseConnectionOptions.SectionName);
        services.TryAddSingleton(Options.Create(options));
        services.TryAddSingleton<ILogEventRepository, MsSqlLogEventRepository>();
        return services;
    }
}