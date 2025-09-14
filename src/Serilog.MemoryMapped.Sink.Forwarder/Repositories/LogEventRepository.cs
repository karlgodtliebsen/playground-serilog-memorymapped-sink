using Dapper;

using Microsoft.Extensions.Logging;

using Serilog.MemoryMapped.Sink.Sinks;

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;

public abstract class LogEventRepository(Microsoft.Extensions.Logging.ILogger logger) : ILogEventRepository
{
    protected abstract string GetConnectionString();

    protected abstract DbConnection GetConnection();
    protected abstract string GetCreateTableStatement();

    private void PrintInformation(string message)
    {
        //SelfLog.WriteLine($"Rolling database to {newFilePath}");
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


    public async Task CreateTable(CancellationToken cancellationToken)
    {

        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(GetCreateTableStatement());
            PrintInformation($"Successfully Created log_event Table");
            logger.LogInformation("Successfully Created log_event Table");
        }
        catch (Exception ex)
        {
            PrintError(ex, "Error Creating log_event Table");
            logger.LogError(ex, "Error Creating log_event Table");
        }
    }

    public async IAsyncEnumerable<LogEventWrapper> Find(object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sqlStatement = "SELECT * FROM log_event";
        await using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        IEnumerable<LogEventWrapper> reader = await connection.QueryAsync<LogEventWrapper>(sqlStatement, parameters);
        foreach (var logEvent in reader)
        {
            yield return logEvent;
        }
    }


    public async Task Add(IEnumerable<LogEventWrapper> entries, CancellationToken cancellationToken)
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
                    exception = entity.Exception,
                    rendered_message = entity.RenderedMessage,
                    trace_id = entity.TraceId,
                    span_id = entity.SpanId,
                    properties = entity.Properties,

                }).ToArray();

                var sqlStatement =
                    @" INSERT INTO log_event (timestamp, level, exception, rendered_message, trace_id, span_id,properties) 
                    VALUES (@timestamp, @level, @exception, @rendered_message, @trace_id, @span_id, @properties);";

                var rowsAffected = await connection.ExecuteAsync(sqlStatement, parameters, transaction);

                await transaction.CommitAsync(cancellationToken);

                PrintInformation($"Successfully inserted {rowsAffected} entries into log_event table");
                logger.LogInformation("Successfully inserted {rowsAffected} entries into log_event table", rowsAffected);
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