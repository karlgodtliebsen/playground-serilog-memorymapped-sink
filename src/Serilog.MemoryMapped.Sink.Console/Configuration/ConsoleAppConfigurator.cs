using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Debugging;
using Serilog.MemoryMapped.Sink.Configuration;

namespace Serilog.MemoryMapped.Sink.Console.Configuration;

public static class ConsoleAppConfigurator
{
    public static (IServiceCollection services, IConfiguration configuration) CreateSettings()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(System.Console.WriteLine);

        SelfLog.Enable(msg => { System.Console.WriteLine($"Serilog: {msg}"); });

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
        serviceProvider.SetupSerilogWithSink(configuration);
        return serviceProvider;
    }
}