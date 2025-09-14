namespace Serilog.MemoryMapped.Repository.SqLite.Configuration;

public class DatabaseConnectionOptions
{
    public const string SectionName = "DatabaseConnection";

    public string ConnectionString { get; set; } = null!;
}