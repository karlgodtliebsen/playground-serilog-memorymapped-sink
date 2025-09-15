using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.MemoryMapped.Queue;
using Serilog.MemoryMapped.Sink.Configuration;

namespace Serilog.MemoryMapped.Sink.Console.Configuration;

public static class ConsoleAppConfigurator
{
    public static (IServiceCollection services, IConfiguration configuration) CreateSettings()
    {
        //MemoryMapperLogger.Disable();
        //MemoryMapperLogger.Enable((msg) =>
        //{
        //    //System.Console.WriteLine(msg);
        //    Log.Logger.Verbose("MemoryMapper Logger {message}", msg);
        //});

        // SelfLog.Enable(msg => { System.Console.WriteLine($"Serilog SelfLog: {msg}"); });

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json");

        var configuration = configurationBuilder.Build();
        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices(configuration);
        services.AddLogging((loggingBuilder) => { services.AddSerilog(loggingBuilder, configuration); });
        return (services, configuration);
    }

    public static IServiceProvider Build(IServiceCollection services, IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        // serviceProvider.SetupSerilogWithSink(LogEventLevel.Information);
        return serviceProvider;
    }
}