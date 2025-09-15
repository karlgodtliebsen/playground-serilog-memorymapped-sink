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
    public static IHost BuildApplicationLoggingHostUsingSqLite()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var logger = SerilogConfigurator.CreateConsumerLogger(context.Configuration);
                services.AddSingleton<ILogger>(logger);
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services
                    .AddMemoryMappedServices(context.Configuration)
                    .AddForwarderServices(context.Configuration)
                    .AddHostServices(context.Configuration)
                    .AddSqLiteServices(context.Configuration);
            });

        var host = builder.Build();
        return host;
    }

    //This is dedicated to TestContainer PostgreSql, which is why the connection string is transferred. Can be done in a better way, but beyond the scope
    public static IHost BuildApplicationLoggingHostUsingPostgreSql(string connectionString)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var logger = SerilogConfigurator.CreateConsumerLogger(context.Configuration);
                services.AddSingleton<ILogger>(logger);
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services
                    .AddMemoryMappedServices(context.Configuration)
                    .AddForwarderServices(context.Configuration)
                    .AddHostServices(context.Configuration)
                    .AddPostgreSqlServices(context.Configuration, connectionString);
            });

        var host = builder.Build();
        return host;
    }

    public static IHost BuildApplicationLoggingHostUsingMsSql()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var logger = SerilogConfigurator.CreateConsumerLogger(context.Configuration);
                services.AddSingleton<ILogger>(logger);
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services
                    .AddMemoryMappedServices(context.Configuration)
                    .AddForwarderServices(context.Configuration)
                    .AddHostServices(context.Configuration)
                    .AddMsSqlServices(context.Configuration);
            });

        var host = builder.Build();

        return host;
    }

    public static IHost BuildProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddMemoryMappedServices(context.Configuration);
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<LogProducerConsoleHost>();
            });

        var host = builder.Build();
        host.Services.SetupSerilogWithSink();
        return host;
    }

    public static IHost BuildMonitorHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var logger = SerilogConfigurator.CreateMonitoringLogger(context.Configuration);
                services.AddSingleton<ILogger>(logger);
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddMemoryMappedQueueServices(context.Configuration);
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