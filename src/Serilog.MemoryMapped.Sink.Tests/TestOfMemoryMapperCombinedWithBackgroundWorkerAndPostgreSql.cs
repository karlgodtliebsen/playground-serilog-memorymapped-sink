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
using Testcontainers.PostgreSql;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorkerAndPostgreSql(ITestOutputHelper output)
{
    //TestContext.Current.CancellationToken
    private readonly CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMinutes(1));

    private readonly string databaseName = "LoggingDemo";
    private readonly string password = "yourStrong(!)Password";
    private PostgreSqlContainer? container = null;
    private readonly IList<string> messages = new List<string>();

    private async Task<string> StartContainerAsync()
    {
        // Initialize and start the container
        container = new PostgreSqlBuilder()
            .WithDatabase(databaseName)
            .WithPassword(password) // SQL Server requires a strong password
            .Build();

        await container.StartAsync();
        var connectionString = container!.GetConnectionString();
        return connectionString;
    }

    [Fact]
    public async Task VerifyLogEventIsEnqueuedInMemoryMapperUsingLogger()
    {
        MemoryMapperLogger.Disable();
        MemoryMapperLogger.Enable(output.WriteLine);
        SelfLog.Enable(msg =>
        {
            if (msg.Contains("Successfully inserted")) messages.Add(msg);
            if (TestContext.Current.CancellationToken.IsCancellationRequested) return;
            output.WriteLine($"Serilog: {msg}");
        });

        var max = 11;
        var count = 0;
        var connectionString = await StartContainerAsync();
        var producer = HostConfigurator.BuildProducerHost();
        var logger = producer.Services.GetRequiredService<ILogger<TestOfMemoryMapperCombinedWithBackgroundWorkerAndSqLite>>();
        var host = HostConfigurator.BuildApplicationLoggingHostUsingPostgreSql(connectionString);


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

                await Task.Delay(10);
            }
        }, TestContext.Current.CancellationToken);

        LogProducer.Produce(output.WriteLine, logger);

        await Task.WhenAny(messageReceived.Task, Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token));
        output.WriteLine($"Done Wait {count}");
        await Log.CloseAndFlushAsync();
        count.Should().BeGreaterThanOrEqualTo(max);
    }
}