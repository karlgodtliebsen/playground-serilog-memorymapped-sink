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

public class TestOfMemoryMapperCombinedWithBackgroundWorkerAndMsSql(ITestOutputHelper output)
{
    //TestContext.Current.CancellationToken
    private IList<string> messages = new List<string>();
    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

    private const string MappedFileName = "thename";
    private (IServiceProvider serviceProvider, IConfiguration configuration) BuildSettings()
    {

        SelfLog.Enable(msg =>
        {
            if (msg.Contains("Successfully inserted")) messages.Add(msg);
            output.WriteLine($"Serilog: {msg}");
        });
        var configurationBuilder = new ConfigurationBuilder();
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
    public async Task VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {
        int max = 11;
        int count = 0;
        var (serviceProvider, configuration) = BuildSettings();
        var host = serviceProvider.BuildApplicationLoggingHostUsingMsSql(configuration, MappedFileName);
        Task.Run(async () => await host.RunAsync(CancellationToken.None));

        using var activity = new Activity("TestOperation")
            .SetIdFormat(ActivityIdFormat.W3C) // Use W3C format
            .Start();

        activity.SetParentId("00-12345678901234567890123456789012-1234567890123456-01");
        // Add some tags to the activity
        activity?.SetTag("test.method", "Test_With_Activity_Tracing_MsSql");
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
        var messageReceived = new TaskCompletionSource<bool>();
        _ = Task.Run(() =>
        {
            if (messages.Count >= max)
            {
                count = messages.Count;
                messageReceived.SetResult(true);
            }
        }, cancellationTokenSource.Token);
        await Task.WhenAny(messageReceived.Task, Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token));
        output.WriteLine($"Done Wait {count}");
        await Log.CloseAndFlushAsync();

        messages.Count.Should().BeGreaterThanOrEqualTo(count);
    }
}