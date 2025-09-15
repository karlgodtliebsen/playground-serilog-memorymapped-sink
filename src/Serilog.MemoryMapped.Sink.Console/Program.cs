
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.MemoryMapped.Queue;
using Serilog.MemoryMapped.Sink.Console.Configuration;

const string title = "MemoryMapped Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

//start a test container instance for postgresql. get the connection string and pass along
//var serilogHost = HostConfigurator.BuildApplicationLoggingHostUsingSqLite();
//var postgreSqlHost = HostConfigurator.BuildApplicationLoggingHostUsingPostgreSql();

var mssqlHost = HostConfigurator.BuildApplicationLoggingHostUsingMsSql();
var monitorHost = HostConfigurator.BuildMonitorHost();
var producerHost = HostConfigurator.BuildProducerHost();
var sLogger = mssqlHost.Services.GetRequiredService<Serilog.ILogger>();
var mLogger = mssqlHost.Services.GetRequiredService<ILogger<Program>>();

MemoryMapperLogger.Disable();
MemoryMapperLogger.Enable((msg) =>
{
    //System.Console.WriteLine(msg);
    sLogger.Verbose("MemoryMapper Logger {message}", msg);
});


//start multiple hosts
await HostConfigurator.RunHostsAsync([mssqlHost, monitorHost, producerHost], title, mLogger, cancellationTokenSource.Token);


