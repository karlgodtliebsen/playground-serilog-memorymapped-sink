using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Repository.PostgreSql.Repositories;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

namespace Serilog.MemoryMapped.Repository.PostgreSql.Configuration;

public static class RepositoryConfigurator
{
    public static IServiceCollection AddPostgreSqlServices(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        var options = configuration.GetSection(DatabaseConnectionOptions.SectionName + "_PostgreSql").Get<DatabaseConnectionOptions>();
        if (options is null) throw new ArgumentNullException(DatabaseConnectionOptions.SectionName);
        options.ConnectionString = connectionString;
        services.TryAddSingleton(Options.Create(options));
        services.TryAddSingleton<ILogEventRepository, PostgreSqlLogEventRepository>();
        return services;
    }
}