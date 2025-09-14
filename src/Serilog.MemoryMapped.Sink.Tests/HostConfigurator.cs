using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog.MemoryMapped.Repository.MsSql.Configuration;
using Serilog.MemoryMapped.Repository.PostgreSql.Configuration;
using Serilog.MemoryMapped.Repository.SqLite.Configuration;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

namespace Serilog.MemoryMapped.Sink.Tests;

public static class HostConfigurator
{
    public static IHost BuildApplicationLoggingHostUsingSqLite(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    services.AddSerilog(loggingBuilder, configuration);
                });
                services
                    .AddMemoryMappedServices(configuration)
                    .AddForwarderServices(configuration)
                    .AddHostServices(configuration)
                    .AddSqLiteServices(configuration);
            });

        var host = builder.Build();
        return host;
    }

    //This is dedicated to TestContainer PostgreSql, which is why the connection string is transfered. Can eb done in a better way, but beyond the scope
    public static IHost BuildApplicationLoggingHostUsingPostgreSql(this IServiceProvider serviceProvider, IConfiguration configuration, string connectionString)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    services.AddSerilog(loggingBuilder, configuration);
                });
                services
                    .AddMemoryMappedServices(configuration)
                    .AddForwarderServices(configuration)
                    .AddHostServices(configuration)
                    .AddPostgreSqlServices(configuration, connectionString);
            });

        var host = builder.Build();
        return host;
    }

    public static IHost BuildApplicationLoggingHostUsingMsSql(this IServiceProvider serviceProvider, IConfiguration configuration)
    {

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    services.AddSerilog(loggingBuilder, configuration);
                });
                services
                    .AddMemoryMappedServices(configuration)
                    .AddForwarderServices(configuration)
                    .AddHostServices(configuration)
                    .AddMsSqlServices(configuration);
            });

        var host = builder.Build();
        return host;
    }
}