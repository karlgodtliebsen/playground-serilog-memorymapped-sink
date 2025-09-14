using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog.Context;
using Serilog.Debugging;
using Serilog.MemoryMapped.Sink.Configuration;

using System.Diagnostics;

using Xunit.Abstractions;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorkerAndSqLite(ITestOutputHelper output)
{
    //TestContext.Current.CancellationToken
    private IList<string> messages = new List<string>();

    private const string MappedFileName = "thename";
    private (IServiceProvider serviceProvider, IConfiguration configuration) BuildSettings()
    {

        SelfLog.Enable(msg =>
        {
            if (msg.Contains("Successfully inserted")) messages.Add(msg);
            output.WriteLine($"Serilog: {msg}");
        }); var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json");
        var configuration = configurationBuilder.Build();
        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices(MappedFileName);

        services.AddLogging((loggingBuilder) => { services.AddSerilog(loggingBuilder, configuration); });

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.SetupSerilogWithSink(configuration);
        return (serviceProvider, configuration);
    }


    [Fact]
    public async Task ProduceOnly()
    {
        var (serviceProvider, configuration) = BuildSettings();

        for (int i = 0; i < 10000; i++)
        {
            Log.Logger.Verbose("the Verbose message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
            Log.Logger.Information("the Information message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
        }
        output.WriteLine($"Done Emitting - Entering Wait");

        await Log.CloseAndFlushAsync();
        await Task.Delay(100);
    }

    [Fact]
    public async Task ConsumeOnly()
    {
        var (serviceProvider, configuration) = BuildSettings();
        var host = serviceProvider.BuildApplicationLoggingHostUsingSqLite(configuration, MappedFileName);
        Task.Run(async () => await host.RunAsync(CancellationToken.None));//not the best way to wait. we should have some task completion wait going on

        output.WriteLine($"Waiting");
        await Task.Delay(10000);
        output.WriteLine($"Done Wait");
    }

    [Fact]
    public async Task VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {
        var (serviceProvider, configuration) = BuildSettings();
        var host = serviceProvider.BuildApplicationLoggingHostUsingSqLite(configuration, MappedFileName);
        Task.Run(async () => await host.RunAsync(CancellationToken.None));

        using var activity = new Activity("TestOperation")
            .SetIdFormat(ActivityIdFormat.W3C) // Use W3C format
            .Start();

        activity.SetParentId("00-12345678901234567890123456789012-1234567890123456-01");
        // Add some tags to the activity
        activity?.SetTag("test.method", "Test_With_Activity_Tracing_SqLite");
        activity?.SetTag("test.class", nameof(TestOfMemoryMapperCombinedWithBackgroundWorkerAndMsSql));

        // Create listener for the activity source
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        for (int i = 0; i < 1000; i++)
        {
            using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
            using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString()))
            {
                Log.Logger.Verbose("the Verbose message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
                Log.Logger.Information("the Information message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1", "the t2", "the t3", i);
            }
            output.WriteLine($"Emitting LogEntries {i}");
        }
        output.WriteLine($"Done Emitting - Entering Wait");

        await Task.Delay(10000);
        output.WriteLine($"Done Wait");
        await Log.CloseAndFlushAsync();
        messages.Count.Should().Be(11);
    }
}