namespace Hartonomous.Api.DTOs.Ingestion;

public class DatabaseIngestionRequest
{
    public required string ConnectionString { get; set; }
    public int TenantId { get; set; } = 0;
    public int MaxTables { get; set; } = 50;
    public int MaxRowsPerTable { get; set; } = 1000;
}
