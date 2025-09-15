using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.MemoryMapped.Queue.Configuration;
using Serilog.MemoryMapped.Repository.MsSql.Configuration;
using Serilog.MemoryMapped.Repository.PostgreSql.Configuration;
using Serilog.MemoryMapped.Repository.SqLite.Configuration;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

namespace Serilog.MemoryMapped.Sink.Console.Configuration;

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

    //This is dedicated to TestContainer PostgreSql, which is why the connection string is transferred. Can be done in a better way, but beyond the scope
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

    public static IHost BuildHost(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, configuration); });
                services.AddHostedService<LogProducerConsoleHost>();
            });

        var host = builder.Build();
        return host;
    }

    public static IHost BuildMonitorHost(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, configuration); });
                services.AddMemoryMappedQueueServices(configuration);
                services.AddHostedService<LogMonitorServiceHost>();
            });

        var host = builder.Build();
        return host;
    }

    public static Task RunHostAsync(IHost host, string title, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            return host.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error starting Host for {title}", title);
            throw;
        }
    }

    public static Task RunHostsAsync(IEnumerable<IHost> hosts, string title, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            IList<Task> tasks = new List<Task>();
            foreach (var host in hosts)
            {
                tasks.Add(host.RunAsync(cancellationToken));
            }

            return Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error starting Hosts for {title}", title);
            throw;
        }
    }
}