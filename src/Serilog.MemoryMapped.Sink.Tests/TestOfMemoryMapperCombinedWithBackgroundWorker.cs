namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorker
{
    [Fact]
    public void Test1()
    {
        ////$"Data Source={name};Journal Mode = WAL;";  
    }
}


//public static class SerilogConfigurator
//{
//    public static void AddSerilog(this IServiceCollection services, ILoggingBuilder loggingBuilder, IConfiguration configuration, Action<IServiceCollection, ILoggingBuilder, IConfiguration>? optionsAction = null)
//    {
//        loggingBuilder.ClearProviders();
//        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
//        loggingBuilder.AddSerilog();
//        //services.AddExtendedLoggingServices(configuration);
//        optionsAction?.Invoke(services, loggingBuilder, configuration);
//    }


//    public static Serilog.ILogger SetupSerilog(this IServiceProvider serviceProvider, IConfiguration configuration, Action<LoggerConfiguration>? action = null)
//    {
//        //TODO: look into sub loggers and filters
//        var logConfig = new LoggerConfiguration();
//        logConfig = logConfig
//            .Enrich.WithMachineName()
//            .Enrich.WithThreadId()
//            .Enrich.FromLogContext()
//            .Enrich.WithSpan()
//            .Enrich.With<TraceIdEnricher>();

//       // logConfig.WriteTo.SetupApplicationLoggingSink(serviceProvider);

//        action?.Invoke(logConfig);

//        Log.Logger = logConfig
//            .ReadFrom.Configuration(configuration)
//            .CreateLogger();
//        return Log.Logger;
//    }
//}