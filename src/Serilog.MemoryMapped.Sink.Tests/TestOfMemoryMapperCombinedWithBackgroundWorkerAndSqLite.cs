using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog.Debugging;
using Serilog.MemoryMapped.Sink.Configuration;

using Xunit.Abstractions;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorkerAndSqLite(ITestOutputHelper output)
{
    //TestContext.Current.CancellationToken
    private readonly IList<string> messages = new List<string>();

    private readonly CancellationTokenSource cancellationTokenSource =
        new CancellationTokenSource(TimeSpan.FromMinutes(1));

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
    public async Task ProduceOnly()
    {
        var (serviceProvider, configuration) = BuildSettings();

        for (int i = 0; i < 10000; i++)
        {
            Log.Logger.Verbose("the Verbose message template {UserId} {t1} {t2} {t3} {index}", "the user", "the t1",
                "the t2", "the t3", i);
            Log.Logger.Information("the Information message template {UserId} {t1} {t2} {t3} {index}", "the user",
                "the t1", "the t2", "the t3", i);
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
        Task.Run(async () =>
            await host.RunAsync(cancellationTokenSource
                .Token)); //not the best way to wait. we should have some task completion wait going on

        output.WriteLine($"Waiting");
        await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token));
        output.WriteLine($"Done Wait");
    }

    [Fact]
    public async Task VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {
        int max = 11;
        int count = 0;
        var (serviceProvider, configuration) = BuildSettings();
        var host = serviceProvider.BuildApplicationLoggingHostUsingSqLite(configuration, MappedFileName);
        Task.Run(async () => await host.RunAsync(cancellationTokenSource.Token));
        var messageReceived = new TaskCompletionSource<bool>();
        _ = Task.Run(async () =>
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (messages.Count >= max)
                {
                    count = messages.Count;
                    messageReceived.SetResult(true);
                }

                await Task.Delay(10);
            }
        }, cancellationTokenSource.Token);

        LogProducer.Produce(output);

        await Task.WhenAny(messageReceived.Task, Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token));
        output.WriteLine($"Done Wait {count}");
        await Log.CloseAndFlushAsync();
        count.Should().BeGreaterThanOrEqualTo(max);
        await cancellationTokenSource.CancelAsync();
    }
}