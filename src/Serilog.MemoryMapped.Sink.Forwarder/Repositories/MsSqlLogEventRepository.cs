using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

using System.Data.Common;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;

public sealed class MsSqlLogEventRepository(
    IOptions<DatabaseConnectionOptions> options,
    ILogger<MsSqlLogEventRepository> logger)
    : LogEventRepository(options, logger)
{
    protected override string GetConnectionString()
    {
        return connectionString;
    }

    protected override string GetCreateTableStatement()
    {
        return @"
CREATE TABLE log_event (    
    timestamp TEXT NOT NULL,
    level TEXT NOT NULL,
    exception TEXT NULL,
    rendered_message TEXT NOT NULL,    
    trace_id TEXT NULL,
    span_id TEXT NULL,
    properties TEXT NULL
);
";
    }

    protected override DbConnection GetConnection()
    {
        return new SqlConnection(connectionString);
    }
    private readonly string connectionString = options.Value.ConnectionString;
}