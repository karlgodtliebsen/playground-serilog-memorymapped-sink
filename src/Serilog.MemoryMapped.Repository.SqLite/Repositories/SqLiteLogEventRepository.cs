using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Repository.SqLite.Configuration;
using Serilog.MemoryMapped.Sink.Forwarder.Repositories;

using System.Data.Common;
using System.Data.SQLite;

namespace Serilog.MemoryMapped.Repository.SqLite.Repositories;

public sealed class SqLiteLogEventRepository : LogEventRepository
{
    private readonly string connectionString;

    public SqLiteLogEventRepository(IOptions<DatabaseConnectionOptions> options, ILogger<SqLiteLogEventRepository> logger) : base(logger)
    {
        connectionString = BuildSqLiteConnectionString(options.Value.ConnectionString);
    }

    protected override string GetCreateTableStatement()
    {
        return @"CREATE TABLE IF NOT EXISTS 
    log_event (    
    timestamp TEXT NOT NULL,
    level VARCHAR(10) NOT NULL,
    exception TEXT NULL,
    rendered_message TEXT NOT NULL,    
    trace_id TEXT NULL,
    span_id TEXT NULL,
    properties TEXT NULL
);
";
    }
    //private readonly uint maxDatabaseSize = 10;
    //private const long BytesPerMb = 1_048_576;
    //private const long MaxSupportedPages = 5_242_880;
    //private const long MaxSupportedPageSize = 4096;
    //private const long MaxSupportedDatabaseSize = unchecked(MaxSupportedPageSize * MaxSupportedPages) / 1048576;


    private string BuildSqLiteConnectionString(string connString)
    {
        var sqlConString = new SQLiteConnectionStringBuilder
        {
            DataSource = connString,
            JournalMode = SQLiteJournalModeEnum.Wal,
            SyncMode = SynchronizationModes.Full,
            //CacheSize = 500,
            //PageSize = (int)MaxSupportedPageSize,
            //MaxPageCount = (int)(maxDatabaseSize * BytesPerMb / MaxSupportedPageSize)
        }.ConnectionString;
        return sqlConString;
    }

    protected override string GetConnectionString()
    {
        return connectionString;
    }
    protected override DbConnection GetConnection()
    {
        return new SQLiteConnection(connectionString);
    }


}