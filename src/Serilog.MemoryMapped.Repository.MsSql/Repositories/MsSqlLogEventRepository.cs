using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Sink.Forwarder.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

using System.Data.Common;

namespace Serilog.MemoryMapped.Repository.MsSql.Repositories;

public sealed class MsSqlLogEventRepository(
    IOptions<DatabaseConnectionOptions> options,
    ILogger<MsSqlLogEventRepository> logger)
    : LogEventRepository(logger)
{
    private readonly string connectionString = options.Value.ConnectionString;

    protected override string GetConnectionString()
    {
        return connectionString;
    }

    protected override string GetCreateTableStatement()
    {
        return @"
IF OBJECT_ID('dbo.log_event', 'U') IS NULL
BEGIN
    CREATE TABLE log_event (    
        id bigint IDENTITY(1,1) PRIMARY KEY, 
        [created_at] datetimeoffset(7) NOT NULL DEFAULT getdate(),
        [timestamp] datetimeoffset(7) NOT NULL,
        [level] nvarchar(25) NOT NULL,
        [exception] nvarchar(max) NULL,
        rendered_message nvarchar(max) NOT NULL,    
        message_template nvarchar(max) NOT NULL,    
        trace_id nvarchar(255) NULL,
        span_id nvarchar(255) NULL,
        properties nvarchar(max) NULL
    );
   
    CREATE NONCLUSTERED INDEX IX_log_event_timestamp ON log_event ([timestamp]);
    CREATE NONCLUSTERED INDEX IX_log_event_level ON log_event ([level]);
    CREATE NONCLUSTERED INDEX IX_log_event_trace_id ON log_event (trace_id) WHERE trace_id IS NOT NULL;
  
END;
";
    }

    protected override DbConnection GetConnection()
    {
        return new SqlConnection(connectionString);
    }

}