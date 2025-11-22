using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Git;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes Git repository metadata: commits, branches, file history, diffs.
/// Preserves version control structure and relationships.
/// </summary>
public class GitRepositoryAtomizer : IAtomizer<GitRepositoryInfo>
{
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IEnumerable<IAtomizer<byte[]>> _byteAtomizers;
    private const int MaxAtomSize = 64;
    public int Priority => 45;

    public GitRepositoryAtomizer(
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> byteAtomizers)
    {
        _fileTypeDetector = fileTypeDetector;
        _byteAtomizers = byteAtomizers;
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        return false; // Invoked explicitly via GitRepositoryInfo
    }

    public async Task<AtomizationResult> AtomizeAsync(
        GitRepositoryInfo repoInfo,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            if (!Directory.Exists(repoInfo.RepositoryPath))
            {
                throw new DirectoryNotFoundException($"Git repository not found: {repoInfo.RepositoryPath}");
            }

            // Verify it's a git repository
            var gitDir = Path.Combine(repoInfo.RepositoryPath, ".git");
            if (!Directory.Exists(gitDir))
            {
                throw new InvalidOperationException($"Not a git repository: {repoInfo.RepositoryPath}");
            }

            // Create repository name atom
            var repoName = Path.GetFileName(repoInfo.RepositoryPath);
            var repoBytes = Encoding.UTF8.GetBytes(repoName);
            var repoHash = SHA256.HashData(repoBytes);
            var repoAtom = new AtomData
            {
                AtomicValue = repoBytes,
                ContentHash = repoHash,
                Modality = "git",
                Subtype = "repository-name",
                ContentType = "application/x-git",
                CanonicalText = repoName,
                Metadata = $"{{\"path\":\"{repoInfo.RepositoryPath}\",\"name\":\"{repoName}\"}}"
            };
            atoms.Add(repoAtom);

            // Get branches
            var branches = await GetBranchesAsync(repoInfo.RepositoryPath, cancellationToken);
            int branchIndex = 0;
            foreach (var branch in branches.Take(repoInfo.MaxBranches))
            {
                var branchBytes = Encoding.UTF8.GetBytes(branch);
                var branchHash = SHA256.HashData(branchBytes);
                var branchAtom = new AtomData
                {
                    AtomicValue = branchBytes,
                    ContentHash = branchHash,
                    Modality = "git",
                    Subtype = "branch-name",
                    ContentType = "application/x-git",
                    CanonicalText = branch,
                    Metadata = $"{{\"branch\":\"{branch}\",\"repository\":\"{repoName}\"}}"
                };
                atoms.Add(branchAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = repoHash,
                    ComponentAtomHash = branchHash,
                    SequenceIndex = branchIndex++,
                    Position = new SpatialPosition { X = 0, Y = branchIndex, Z = 0 }
                });
            }

            // Get commits
            var commits = await GetCommitsAsync(repoInfo.RepositoryPath, repoInfo.MaxCommits, cancellationToken);
            int commitIndex = 0;
            foreach (var commit in commits)
            {
                var commitBytes = Encoding.UTF8.GetBytes(commit.Hash);
                var commitHash = SHA256.HashData(commitBytes);
                var commitAtom = new AtomData
                {
                    AtomicValue = commitBytes,
                    ContentHash = commitHash,
                    Modality = "git",
                    Subtype = "commit-hash",
                    ContentType = "application/x-git",
                    CanonicalText = $"{commit.Hash[..8]} - {commit.Message}",
                    Metadata = $"{{\"hash\":\"{commit.Hash}\",\"author\":\"{commit.Author}\",\"date\":\"{commit.Date}\",\"message\":\"{EscapeJson(commit.Message)}\"}}"
                };
                atoms.Add(commitAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = repoHash,
                    ComponentAtomHash = commitHash,
                    SequenceIndex = commitIndex++,
                    Position = new SpatialPosition { X = 0, Y = commitIndex, Z = 0 }
                });

                // Atomize commit message into words/tokens
                var messageWords = commit.Message.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                byte[]? lastWordHash = null; // Track last word hash to link commit message
                
                foreach (var word in messageWords)
                {
                    var wordBytes = Encoding.UTF8.GetBytes(word);
                    if (wordBytes.Length > MaxAtomSize)
                        wordBytes = wordBytes.Take(MaxAtomSize).ToArray();
                    
                    var wordHash = SHA256.HashData(wordBytes);
                    lastWordHash = wordHash; // Remember for linking
                    
                    if (!atoms.Any(a => a.ContentHash.SequenceEqual(wordHash)))
                    {
                        var wordAtom = new AtomData
                        {
                            AtomicValue = wordBytes,
                            ContentHash = wordHash,
                            Modality = "git",
                            Subtype = "commit-message-word",
                            ContentType = "text/plain",
                            CanonicalText = word,
                            Metadata = $"{{\"commit\":\"{commit.Hash[..8]}\"}}"
                        };
                        atoms.Add(wordAtom);
                    }
                    
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = commitHash,
                        ComponentAtomHash = wordHash,
                        SequenceIndex = compositions.Count,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }

                // Link commit to last message word if any
                if (lastWordHash != null)
                {
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = commitHash,
                        ComponentAtomHash = lastWordHash,
                        SequenceIndex = 0,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }
            }

            // Get file history (if enabled)
            if (repoInfo.IncludeFileHistory)
            {
                var files = await GetTrackedFilesAsync(repoInfo.RepositoryPath, cancellationToken);
                int fileIndex = 0;
                
                foreach (var file in files.Take(repoInfo.MaxFiles))
                {
                    var fileBytes = Encoding.UTF8.GetBytes(file);
                    var fileHash = SHA256.HashData(fileBytes);
                    var fileAtom = new AtomData
                    {
                        AtomicValue = fileBytes.Length <= MaxAtomSize ? fileBytes : fileBytes.Take(MaxAtomSize).ToArray(),
                        ContentHash = fileHash,
                        Modality = "git",
                        Subtype = "tracked-file",
                        ContentType = "application/x-git",
                        CanonicalText = file,
                        Metadata = $"{{\"path\":\"{file}\"}}"
                    };
                    atoms.Add(fileAtom);

                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = repoHash,
                        ComponentAtomHash = fileHash,
                        SequenceIndex = fileIndex++,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = fileIndex }
                    });
                }
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count(),
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(GitRepositoryAtomizer),
                    DetectedFormat = $"Git Repository - {branches.Count} branches, {commits.Count} commits",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Git repository atomization failed: {ex.Message}");
            throw;
        }
    }

    private async Task<List<string>> GetBranchesAsync(string repoPath, CancellationToken cancellationToken)
    {
        var output = await RunGitCommandAsync(repoPath, "branch -a", cancellationToken);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim().TrimStart('*').Trim())
            .ToList();
    }

    private async Task<List<CommitInfo>> GetCommitsAsync(string repoPath, int maxCommits, CancellationToken cancellationToken)
    {
        var output = await RunGitCommandAsync(
            repoPath,
            $"log --pretty=format:\"%H|%an|%ad|%s\" --date=iso -n {maxCommits}",
            cancellationToken);

        var commits = new List<CommitInfo>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length >= 4)
            {
                commits.Add(new CommitInfo
                {
                    Hash = parts[0],
                    Author = parts[1],
                    Date = parts[2],
                    Message = parts[3]
                });
            }
        }

        return commits;
    }

    private async Task<List<string>> GetTrackedFilesAsync(string repoPath, CancellationToken cancellationToken)
    {
        var output = await RunGitCommandAsync(repoPath, "ls-files", cancellationToken);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private async Task<string> RunGitCommandAsync(string repoPath, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }

    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private class CommitInfo
    {
        public string Hash { get; set; } = "";
        public string Author { get; set; } = "";
        public string Date { get; set; } = "";
        public string Message { get; set; } = "";
    }
}

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
