using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

using Serilog.MemoryMapped.Sink.Forwarder.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

using System.Data.Common;

namespace Serilog.MemoryMapped.Repository.PostgreSql.Repositories;

public sealed class PostgreSqlLogEventRepository : LogEventRepository
{
    private readonly string connectionString;
    public PostgreSqlLogEventRepository(IOptions<DatabaseConnectionOptions> options, ILogger<PostgreSqlLogEventRepository> logger) : base(logger)
    {
        connectionString = BuildConnectionString(options.Value.ConnectionString);
    }

    protected override string GetConnectionString()
    {
        return connectionString;
    }

    private string BuildConnectionString(string connString)
    {
        var sqlConString = new NpgsqlConnectionStringBuilder
        {
            ConnectionString = connString
        }.ConnectionString;
        return sqlConString;
    }


    protected override string GetCreateTableStatement()
    {
        return @"
CREATE TABLE IF NOT EXISTS log_event (    
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    timestamp TIMESTAMPTZ NOT NULL,
    level VARCHAR(25) NOT NULL CHECK (level IN ('Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal')),
    exception TEXT NULL,
    rendered_message TEXT NOT NULL,
    message_template TEXT NOT NULL,
    trace_id VARCHAR(255) NULL,
    span_id VARCHAR(255) NULL,
    properties TEXT NULL, 
    search_vector TSVECTOR,                                        
    created_at_date DATE GENERATED ALWAYS AS (DATE(created_at AT TIME ZONE 'UTC')) STORED
);

-- Enhanced indexes for PostgreSQL
CREATE INDEX IF NOT EXISTS ix_log_timestamp ON log_event USING BTREE (timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_log_level ON log_event (level);
CREATE INDEX IF NOT EXISTS ix_log_trace_id ON log_event (trace_id) WHERE trace_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_log_date_level ON log_event (created_at_date, level); 
";
    }

    protected override DbConnection GetConnection()
    {
        return new NpgsqlConnection(connectionString);
    }

}