using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog.Context;
using Serilog.Debugging;
using Serilog.MemoryMapped.Sink.Configuration;

using System.Diagnostics;

using Testcontainers.PostgreSql;

using Xunit.Abstractions;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapperCombinedWithBackgroundWorkerAndPostgreSql(ITestOutputHelper output)
{
    //TestContext.Current.CancellationToken
    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

    private const string MappedFileName = "thename";
    private readonly string databaseName = "LoggingDemo";
    private readonly string password = "yourStrong(!)Password";
    private PostgreSqlContainer? container = null;
    private IList<string> messages = new List<string>();

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
    private (IServiceProvider serviceProvider, IConfiguration configuration) BuildSettings(string connectionString)
    {
        output.WriteLine($"Using PostgreSql TestContainer Connection: {connectionString}");
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
        var connectionString = await StartContainerAsync();
        var (serviceProvider, configuration) = BuildSettings(connectionString);


        var host = serviceProvider.BuildApplicationLoggingHostUsingPostgreSql(configuration, MappedFileName, connectionString);
        Task.Run(async () => await host.RunAsync(CancellationToken.None));

        using var activity = new Activity("TestOperation")
            .SetIdFormat(ActivityIdFormat.W3C) // Use W3C format
            .Start();

        activity.SetParentId("00-12345678901234567890123456789012-1234567890123456-01");
        // Add some tags to the activity
        activity?.SetTag("test.method", "Test_With_Activity_Tracing_PostgreSql");
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