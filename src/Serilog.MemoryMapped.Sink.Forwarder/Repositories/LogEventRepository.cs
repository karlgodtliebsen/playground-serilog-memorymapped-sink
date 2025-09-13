using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.Events;
using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;

public abstract class LogEventRepository(IOptions<DatabaseConnectionOptions> options, Microsoft.Extensions.Logging.ILogger logger) : ILogEventRepository
{
    protected abstract string GetConnectionString();

    protected abstract DbConnection GetConnection();

    private void PrintInformation(string message)
    {
        Debug.Print(message);
        Trace.WriteLine(message);
        Console.WriteLine(message);
    }
    private void PrintError(Exception ex, string action)
    {
        Debug.Print($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
        Trace.WriteLine($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
        Console.WriteLine($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
    }

    protected virtual string GetCreateTableStatement()
    {
        return @"
CREATE TABLE log_event (    
    timestamp TEXT NOT NULL,
    level TEXT NOT NULL,
    exception TEXT NULL,
    message_template TEXT NOT NULL,    
    trace_id TEXT NULL,
    span_id TEXT NULL,
    properties TEXT NULL
);
";
    }

    public async Task CreateTable(CancellationToken cancellationToken)
    {

        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(GetCreateTableStatement());
            PrintInformation($"Successfully Created log_event Table");
        }
        catch (Exception ex)
        {
            PrintError(ex, "Error Creating log_event Table");
            logger.LogError(ex, "Error Creating log_event Table");
        }
    }

    public async IAsyncEnumerable<LogEvent> Find(object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sqlStatement = "SELECT * FROM log_event";
        await using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        IEnumerable<LogEvent> reader = await connection.QueryAsync<LogEvent>(sqlStatement, parameters);
        foreach (var logEvent in reader)
        {
            yield return logEvent;
        }
    }


    public async Task Add(IEnumerable<LogEvent> entries, CancellationToken cancellationToken)
    {
        var entriesList = entries.ToList();
        if (!entriesList.Any()) return;

        await using var connection = GetConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var parameters = entriesList.Select(entity => new
                {
                    timestamp = entity.Timestamp,
                    level = entity.Level.ToString(),
                    exception = entity.Exception.ToJson(),
                    message_template = entity.MessageTemplate,
                    trace_id = entity.TraceId,
                    span_id = entity.SpanId,
                    properties = entity.Properties.ToJson(),

                }).ToArray();

                var sqlStatement =
                    @" INSERT INTO log_event (timestamp, level, exception, message_template, trace_id, span_id,properties) 
                    VALUES (@timestamp, @level, @exception, @message_template, @trace_id, @span_id, @properties);";

                var rowsAffected = await connection.ExecuteAsync(sqlStatement, parameters, transaction);

                await transaction.CommitAsync(cancellationToken);

                PrintInformation($"Successfully inserted {rowsAffected} entries into log_event table");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to insert {Count} entries, transaction rolled back", entriesList.Count);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while forwarding {Count} log_event entries", entriesList.Count);
            throw;
        }
    }
    public async Task<bool> TestConnection(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync(cancellationToken);

            // Simple query to test connection
            var result = await connection.QuerySingleAsync<int>("SELECT 1");

            PrintInformation("Connection test successful");
            return result == 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection test failed");
            return false;
        }
    }
}