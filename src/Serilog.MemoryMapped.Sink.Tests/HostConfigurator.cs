using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog.MemoryMapped.Repository.MsSql.Configuration;
using Serilog.MemoryMapped.Repository.SqLite.Configuration;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

namespace Serilog.MemoryMapped.Sink.Tests;

public static class HostConfigurator
{
    public static IHost BuildApplicationLoggingHostUsingSqLite(this IServiceProvider serviceProvider, IConfiguration configuration, string name)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    services.AddSerilog(loggingBuilder, configuration);
                });
                services
                    .AddMemoryMappedServices(name)
                    .AddForwarderServices(configuration)
                    .AddHostServices(configuration)
                    .AddSqLiteServices(configuration);
            });

        var host = builder.Build();
        return host;
    }

    public static IHost BuildApplicationLoggingHostUsingMsSql(this IServiceProvider serviceProvider, IConfiguration configuration, string name)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    services.AddSerilog(loggingBuilder, configuration);
                });
                services
                    .AddMemoryMappedServices(name)
                    .AddForwarderServices(configuration)
                    .AddHostServices(configuration)
                    .AddMsSqlServices(configuration);
            });

        var host = builder.Build();
        return host;
    }
}