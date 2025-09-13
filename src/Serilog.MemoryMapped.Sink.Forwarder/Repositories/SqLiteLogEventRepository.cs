using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.MemoryMapped.Sink.Forwarder.Configuration;

using System.Data.Common;
using System.Data.SQLite;

namespace Serilog.MemoryMapped.Sink.Forwarder.Repositories;


//SqlMapper.AddTypeHandler(typeof(DateTimeOffset), new DateTimeOffsetHandler());
//SqlMapper.AddTypeHandler(typeof(Guid), new GuidHandler());
//SqlMapper.AddTypeHandler(typeof(TimeSpan), new TimeSpanHandler());

public sealed class SqLiteLogEventRepository(
    IOptions<DatabaseConnectionOptions> options,
    ILogger<SqLiteLogEventRepository> logger)
    : LogEventRepository(options, logger)
{
    protected override string GetConnectionString()
    {
        return connectionString;
    }
    protected override DbConnection GetConnection()
    {
        return new SQLiteConnection(connectionString);
    }
    private readonly string connectionString = options.Value.ConnectionString;
}