using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Debugging;
using Serilog.MemoryMapped.Queue;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.MemoryMapped.Sink.Console;
using Serilog.MemoryMapped.Sink.Console.Configuration;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorkerAndSqLite(ITestOutputHelper output)
{
    //TestContext.Current.CancellationToken
    private readonly IList<string> messages = new List<string>();
    private readonly CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMinutes(1));


    private (IServiceProvider serviceProvider, IConfiguration configuration) BuildSettings()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);

        SelfLog.Enable(msg =>
        {
            if (msg.Contains("Successfully inserted")) messages.Add(msg);
            if (TestContext.Current.CancellationToken.IsCancellationRequested) return;
            output.WriteLine($"Serilog: {msg}");
        });
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json");
        var configuration = configurationBuilder.Build();
        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices(configuration);

        services.AddLogging((loggingBuilder) => { services.AddSerilog(loggingBuilder, configuration); });

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.SetupSerilogWithSink(configuration);
        return (serviceProvider, configuration);
    }


    [Fact]
    public async Task ProduceOnly()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        var (serviceProvider, configuration) = BuildSettings();

        for (var i = 0; i < 10000; i++)
        {
            Log.Logger.Verbose("the Verbose message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1",
                "the t2", "the t3", i);
            Log.Logger.Information("the Information message template {UserId} {t1} {t2} {t3} {index}", "the user",
                "the t1", "the t2", "the t3", i);
        }

        output.WriteLine($"Done Emitting - Entering Wait");

        await Log.CloseAndFlushAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConsumeOnly()
    {
        var (serviceProvider, configuration) = BuildSettings();
        var host = serviceProvider.BuildApplicationLoggingHostUsingSqLite(configuration);
        Task.Run(async () => await host.RunAsync(TestContext.Current.CancellationToken)); //not the best way to wait. we should have some task completion wait going on

        output.WriteLine($"Waiting");
        await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        output.WriteLine($"Done Waiting");
    }

    [Fact]
    public async Task VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {
        var max = 11;
        var count = 0;
        var (serviceProvider, configuration) = BuildSettings();
        var host = serviceProvider.BuildApplicationLoggingHostUsingSqLite(configuration);
        Task.Run(async () => await host.RunAsync(TestContext.Current.CancellationToken));
        var messageReceived = new TaskCompletionSource<bool>();
        _ = Task.Run(async () =>
        {
            while (!cancellationTokenSource.IsCancellationRequested && !TestContext.Current.CancellationToken.IsCancellationRequested)
            {
                if (messages.Count >= max)
                {
                    count = messages.Count;
                    messageReceived.SetResult(true);
                }

                await Task.Delay(1);
            }
        }, TestContext.Current.CancellationToken);
        var logger = serviceProvider.GetRequiredService<ILogger<TestOfMemoryMapperCombinedWithBackgroundWorkerAndSqLite>>();

        LogProducer.Produce(output.WriteLine, logger);

        await Task.WhenAny(messageReceived.Task, Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token));
        output.WriteLine($"Done Wait {count}");
        await Log.CloseAndFlushAsync();
        count.Should().BeGreaterThanOrEqualTo(max);
        await cancellationTokenSource.CancelAsync();
    }
}