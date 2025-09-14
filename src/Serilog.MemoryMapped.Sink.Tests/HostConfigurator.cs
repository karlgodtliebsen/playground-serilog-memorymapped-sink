using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.MemoryMapped.Repository.SqLite.Configuration;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

namespace Serilog.MemoryMapped.Sink.Tests;

public static class HostConfigurator
{
    public static IHost BuildApplicationLoggingHost(this IServiceProvider serviceProvider, IConfiguration configuration, string name)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                    // loggingBuilder.AddSerilog();
                    loggingBuilder.AddConsole();
                    loggingBuilder.AddDebug();
                });
                services
                    .AddMemoryMappedServices(name)
                    .AddForwarderServices(configuration)
                    .AddHostServices(configuration)
                    .AddSqLiteServices(configuration);
            });

        var host = builder.Build();
        //host.Services.SetupSerilog(configuration);
        return host;
    }
}