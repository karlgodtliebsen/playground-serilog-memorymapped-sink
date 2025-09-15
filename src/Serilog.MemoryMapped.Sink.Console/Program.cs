// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.MemoryMapped.Sink.Console.Configuration;

var title = "MemoryMapped Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

var (services, configuration) = ConsoleAppConfigurator.CreateSettings();
var serviceProvider = ConsoleAppConfigurator.Build(services, configuration);

//var serilogHost = serviceProvider.BuildApplicationLoggingHostUsingSqLite(configuration);
var mssqlHost = serviceProvider.BuildApplicationLoggingHostUsingMsSql(configuration);

//TODO: start a test container instance for postgresql. get the connection string and pass along
//var postgreSqlHost = serviceProvider.BuildApplicationLoggingHostUsingPostgreSql(configuration);

var host = serviceProvider.BuildHost(configuration);

//TODO: add monitor
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

//start multiple hosts
await HostConfigurator.RunHostsAsync([ /*serilogHost,*/ mssqlHost, host], title, logger, cancellationTokenSource.Token);





