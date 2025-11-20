namespace Hartonomous.Api.DTOs.Ingestion;

public class GitIngestionRequest
{
    public required string RepositoryPath { get; set; }
    public int TenantId { get; set; } = 0;
    public int MaxBranches { get; set; } = 50;
    public int MaxCommits { get; set; } = 100;
    public int MaxFiles { get; set; } = 1000;
    public bool IncludeFileHistory { get; set; } = true;
}
