namespace Hartonomous.Core.Models.Git;

/// <summary>
/// Git repository information for atomization.
/// </summary>
public class GitRepositoryInfo
{
    public required string RepositoryPath { get; set; }
    public int MaxBranches { get; set; } = 50;
    public int MaxCommits { get; set; } = 100;
    public int MaxFiles { get; set; } = 1000;
    public bool IncludeFileHistory { get; set; } = true;
}
