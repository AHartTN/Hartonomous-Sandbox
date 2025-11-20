using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.Infrastructure.FileType;
using Hartonomous.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Data ingestion API - comprehensive atomization of files into 64-byte atoms.
/// Supports text, images, audio, video, documents, archives, models, code, and more.
/// </summary>
[Route("api/v{version:apiVersion}/ingestion")]
public class DataIngestionController : ApiControllerBase
{
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
    private readonly IAtomBulkInsertService _bulkInsertService;
    
    // In-memory job tracking (TODO: move to persistent storage)
    private static readonly ConcurrentDictionary<string, IngestionJob> _jobs = new();

    public DataIngestionController(
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> atomizers,
        IAtomBulkInsertService bulkInsertService,
        ILogger<DataIngestionController> logger)
        : base(logger)
    {
        _fileTypeDetector = fileTypeDetector;
        _atomizers = atomizers;
        _bulkInsertService = bulkInsertService;
    }

    /// <summary>
    /// Ingest a single file with full atomization.
    /// POST /api/ingestion/file
    /// </summary>
    [HttpPost("file")]
    [RequestSizeLimit(1_000_000_000)] // 1GB max
    [EnableRateLimiting("ingestion")]
    public async Task<IActionResult> IngestFile(
        [FromForm] IFormFile file,
        [FromForm] int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return ErrorResult("No file provided", 400);

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = file.FileName,
            FileSizeBytes = file.Length,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
        _jobs[jobId] = job;

        try
        {
            // Read file content
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();

            // Detect file type
            var fileType = _fileTypeDetector.Detect(fileContent, file.FileName);
            job.DetectedType = fileType.ContentType;
            job.DetectedCategory = fileType.Category.ToString();

            Logger.LogInformation(
                "Ingesting file: {FileName} ({SizeBytes} bytes, {ContentType}, {Category})",
                file.FileName,
                file.Length,
                fileType.ContentType,
                fileType.Category);

            // Find appropriate atomizer
            var atomizer = _atomizers
                .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            if (atomizer == null)
            {
                job.Status = "failed";
                job.ErrorMessage = $"No atomizer found for {fileType.ContentType}";
                return ErrorResult(job.ErrorMessage, 400);
            }

            // Atomize file
            var sourceMetadata = new SourceMetadata
            {
                FileName = file.FileName,
                SourceUri = $"upload://{file.FileName}",
                SourceType = "file-upload",
                ContentType = fileType.ContentType,
                SizeBytes = file.Length,
                TenantId = tenantId,
                Metadata = $"{{\"category\":\"{fileType.Category}\",\"format\":\"{fileType.SpecificFormat}\"}}"
            };

            var result = await atomizer.AtomizeAsync(fileContent, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert atoms
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(
                result.Atoms,
                tenantId,
                cancellationToken);

            // Bulk insert compositions
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(
                    result.Compositions,
                    atomIdMap,
                    tenantId,
                    cancellationToken);
            }

            // Handle recursive atomization (archives)
            if (result.ChildSources?.Count > 0)
            {
                job.ChildJobs = new List<string>();
                foreach (var childSource in result.ChildSources)
                {
                    var childJobId = await IngestChildSourceAsync(childSource, tenantId, cancellationToken);
                    job.ChildJobs.Add(childJobId);
                }
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            Logger.LogInformation(
                "Ingestion completed: {JobId}, {TotalAtoms} atoms ({UniqueAtoms} unique), {DurationMs}ms",
                jobId,
                job.TotalAtoms,
                job.UniqueAtoms,
                job.DurationMs);

            return SuccessResult(new
            {
                jobId,
                status = job.Status,
                atoms = new
                {
                    total = job.TotalAtoms,
                    unique = job.UniqueAtoms,
                    deduplicationRate = job.TotalAtoms > 0 
                        ? (1.0 - (double)job.UniqueAtoms / job.TotalAtoms) * 100 
                        : 0
                },
                durationMs = job.DurationMs,
                childJobs = job.ChildJobs
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ingestion failed for {FileName}", file.FileName);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            return ErrorResult(ex.Message, 500);
        }
    }

    /// <summary>
    /// Get ingestion job status and progress.
    /// GET /api/ingestion/jobs/{jobId}
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return ErrorResult("Job not found", 404);

        return SuccessResult(new
        {
            jobId = job.JobId,
            fileName = job.FileName,
            status = job.Status,
            detectedType = job.DetectedType,
            detectedCategory = job.DetectedCategory,
            atoms = new
            {
                total = job.TotalAtoms,
                unique = job.UniqueAtoms
            },
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt,
            durationMs = job.DurationMs,
            childJobs = job.ChildJobs,
            error = job.ErrorMessage
        });
    }

    /// <summary>
    /// Query atoms by content hash.
    /// GET /api/ingestion/atoms?hash={sha256}
    /// </summary>
    [HttpGet("atoms")]
    public IActionResult QueryAtoms([FromQuery] string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return ErrorResult("Hash parameter required", 400);

        // TODO: Query atoms from database
        return SuccessResult(new { message = "Atom query not yet implemented", hash });
    }

    /// <summary>
    /// Ingest content from a URL (web fetch).
    /// POST /api/ingestion/url
    /// </summary>
    [HttpPost("url")]
    public async Task<IActionResult> IngestUrl(
        [FromBody] UrlIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return ErrorResult("URL is required", 400);

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = request.Url,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };
        _jobs[jobId] = job;

        try
        {
            // Use WebFetchAtomizer (IAtomizer<string>)
            var webAtomizer = _atomizers.OfType<IAtomizer<string>>().FirstOrDefault();
            if (webAtomizer == null)
                return ErrorResult("Web fetch atomizer not configured", 500);

            var sourceMetadata = new SourceMetadata
            {
                SourceUri = request.Url,
                SourceType = "web-fetch",
                ContentType = "text/uri-list",
                TenantId = request.TenantId
            };

            var result = await webAtomizer.AtomizeAsync(request.Url, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, request.TenantId, cancellationToken);
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, request.TenantId, cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            return SuccessResult(new
            {
                jobId,
                status = job.Status,
                atoms = new { total = job.TotalAtoms, unique = job.UniqueAtoms },
                durationMs = job.DurationMs
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "URL ingestion failed: {Url}", request.Url);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            return ErrorResult(ex.Message, 500);
        }
    }

    /// <summary>
    /// Ingest database schema and data.
    /// POST /api/ingestion/database
    /// </summary>
    [HttpPost("database")]
    public async Task<IActionResult> IngestDatabase(
        [FromBody] DatabaseIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return ErrorResult("Connection string is required", 400);

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = "Database",
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };
        _jobs[jobId] = job;

        try
        {
            // Use DatabaseAtomizer
            var dbAtomizer = _atomizers.OfType<IAtomizer<DatabaseConnectionInfo>>().FirstOrDefault();
            if (dbAtomizer == null)
                return ErrorResult("Database atomizer not configured", 500);

            var dbInfo = new DatabaseConnectionInfo
            {
                ConnectionString = request.ConnectionString,
                MaxTables = request.MaxTables,
                MaxRowsPerTable = request.MaxRowsPerTable
            };

            var sourceMetadata = new SourceMetadata
            {
                SourceType = "database",
                ContentType = "application/x-sql",
                TenantId = request.TenantId
            };

            var result = await dbAtomizer.AtomizeAsync(dbInfo, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, request.TenantId, cancellationToken);
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, request.TenantId, cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            return SuccessResult(new
            {
                jobId,
                status = job.Status,
                atoms = new { total = job.TotalAtoms, unique = job.UniqueAtoms },
                durationMs = job.DurationMs
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database ingestion failed");
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            return ErrorResult(ex.Message, 500);
        }
    }

    /// <summary>
    /// Ingest Git repository metadata.
    /// POST /api/ingestion/git
    /// </summary>
    [HttpPost("git")]
    public async Task<IActionResult> IngestGitRepository(
        [FromBody] GitIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RepositoryPath))
            return ErrorResult("Repository path is required", 400);

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = request.RepositoryPath,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };
        _jobs[jobId] = job;

        try
        {
            // Use GitRepositoryAtomizer
            var gitAtomizer = _atomizers.OfType<IAtomizer<GitRepositoryInfo>>().FirstOrDefault();
            if (gitAtomizer == null)
                return ErrorResult("Git repository atomizer not configured", 500);

            var gitInfo = new GitRepositoryInfo
            {
                RepositoryPath = request.RepositoryPath,
                MaxBranches = request.MaxBranches,
                MaxCommits = request.MaxCommits,
                MaxFiles = request.MaxFiles,
                IncludeFileHistory = request.IncludeFileHistory
            };

            var sourceMetadata = new SourceMetadata
            {
                SourceUri = request.RepositoryPath,
                SourceType = "git-repository",
                ContentType = "application/x-git",
                TenantId = request.TenantId
            };

            var result = await gitAtomizer.AtomizeAsync(gitInfo, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, request.TenantId, cancellationToken);
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, request.TenantId, cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            return SuccessResult(new
            {
                jobId,
                status = job.Status,
                atoms = new { total = job.TotalAtoms, unique = job.UniqueAtoms },
                durationMs = job.DurationMs
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Git repository ingestion failed: {RepoPath}", request.RepositoryPath);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            return ErrorResult(ex.Message, 500);
        }
    }

    /// <summary>
    /// Recursively ingest child sources (from archives).
    /// </summary>
    private async Task<string> IngestChildSourceAsync(
        ChildSource childSource,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var childJobId = Guid.NewGuid().ToString();
        var childJob = new IngestionJob
        {
            JobId = childJobId,
            FileName = childSource.Metadata.FileName,
            FileSizeBytes = childSource.Content.Length,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
        _jobs[childJobId] = childJob;

        try
        {
            // Detect type
            var fileType = _fileTypeDetector.Detect(childSource.Content, childSource.Metadata.FileName);
            childJob.DetectedType = fileType.ContentType;
            childJob.DetectedCategory = fileType.Category.ToString();

            // Find atomizer
            var atomizer = _atomizers
                .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            if (atomizer != null)
            {
                var result = await atomizer.AtomizeAsync(childSource.Content, childSource.Metadata, cancellationToken);
                childJob.TotalAtoms = result.ProcessingInfo.TotalAtoms;
                childJob.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

                // Bulk insert
                var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, tenantId, cancellationToken);
                if (result.Compositions.Count > 0)
                {
                    await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, tenantId, cancellationToken);
                }

                // Recursive children
                if (result.ChildSources?.Count > 0)
                {
                    childJob.ChildJobs = new List<string>();
                    foreach (var grandchild in result.ChildSources)
                    {
                        var grandchildJobId = await IngestChildSourceAsync(grandchild, tenantId, cancellationToken);
                        childJob.ChildJobs.Add(grandchildJobId);
                    }
                }
            }

            childJob.Status = "completed";
            childJob.CompletedAt = DateTime.UtcNow;
            childJob.DurationMs = (long)(childJob.CompletedAt.Value - childJob.StartedAt).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Child ingestion failed: {FileName}", childSource.Metadata.FileName);
            childJob.Status = "failed";
            childJob.ErrorMessage = ex.Message;
            childJob.CompletedAt = DateTime.UtcNow;
        }

        return childJobId;
    }

    // Simple in-memory job tracking model
    private class IngestionJob
    {
        public string JobId { get; set; } = "";
        public string? FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string Status { get; set; } = "pending";
        public string? DetectedType { get; set; }
        public string? DetectedCategory { get; set; }
        public int TotalAtoms { get; set; }
        public int UniqueAtoms { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long DurationMs { get; set; }
        public List<string>? ChildJobs { get; set; }
        public string? ErrorMessage { get; set; }
        public int TenantId { get; set; }
    }

    // Request models
    public class UrlIngestionRequest
    {
        public required string Url { get; set; }
        public int TenantId { get; set; } = 0;
    }

    public class DatabaseIngestionRequest
    {
        public required string ConnectionString { get; set; }
        public int TenantId { get; set; } = 0;
        public int MaxTables { get; set; } = 50;
        public int MaxRowsPerTable { get; set; } = 1000;
    }

    public class GitIngestionRequest
    {
        public required string RepositoryPath { get; set; }
        public int TenantId { get; set; } = 0;
        public int MaxBranches { get; set; } = 50;
        public int MaxCommits { get; set; } = 100;
        public int MaxFiles { get; set; } = 1000;
        public bool IncludeFileHistory { get; set; } = true;
    }
}
