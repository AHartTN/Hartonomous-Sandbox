using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Hartonomous.DatabaseTests.Fixtures;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlTestcontainer _container;
    private readonly string _saPassword;
    private readonly List<string> _deploymentLog = new();
    private string? _connectionString;

    public bool IsAvailable { get; private set; }

    public string SkipReason { get; private set; } = string.Empty;

    public SqlServerContainerFixture()
    {
        _saPassword = $"P@ssw0rd!{Guid.NewGuid():N}";

        var configuration = new MsSqlTestcontainerConfiguration
        {
            Password = _saPassword
        };

        _container = new TestcontainersBuilder<MsSqlTestcontainer>()
            .WithDatabase(configuration)
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithCleanUp(true)
            .WithName($"hartonomous-sql-{Guid.NewGuid():N}")
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithEnvironment("TZ", "UTC")
            .Build();
    }

    public string ConnectionString => _connectionString ??= BuildHartonomousConnectionString();

    public SqlServerDeploymentBaseline Baseline { get; private set; } = SqlServerDeploymentBaseline.Empty;

    public IReadOnlyList<string> DeploymentLog => _deploymentLog;

    public async Task InitializeAsync()
    {
        if (!IsDockerDaemonAvailable())
        {
            SkipReason = "Docker engine not detected (required named pipe or socket missing).";
            IsAvailable = false;
            return;
        }

        try
        {
            await _container.StartAsync().ConfigureAwait(false);
            await DeployDatabaseAsync().ConfigureAwait(false);
            Baseline = await CaptureBaselineAsync().ConfigureAwait(false);
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            SkipReason = $"Docker SQL fixture unavailable: {ex.Message}";
            IsAvailable = false;
            await SafeDisposeContainerAsync().ConfigureAwait(false);
        }
    }

    public async Task DisposeAsync()
    {
        await SafeDisposeContainerAsync().ConfigureAwait(false);
    }

    private async Task SafeDisposeContainerAsync()
    {
        try
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore dispose failures during teardown
        }
    }

    private static bool IsDockerDaemonAvailable()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return File.Exists(@"\\.\pipe\docker_engine");
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return File.Exists("/var/run/docker.sock");
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private async Task DeployDatabaseAsync()
    {
        var repoRoot = ResolveRepositoryRoot();
        var scriptPath = Path.Combine(repoRoot, "scripts", "deploy-database.ps1");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Deployment script not found", scriptPath);
        }

        var powershellExe = ResolvePowerShellExecutable();

        var publicPort = _container.Port;
        var serverName = $"{_container.Hostname},{publicPort}";

        var startInfo = new ProcessStartInfo
        {
            FileName = powershellExe,
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-ServerName");
        startInfo.ArgumentList.Add(serverName);
        startInfo.ArgumentList.Add("-DatabaseName");
        startInfo.ArgumentList.Add("Hartonomous");
        startInfo.ArgumentList.Add("-SqlUser");
        startInfo.ArgumentList.Add("sa");
        startInfo.ArgumentList.Add("-SqlPassword");
        startInfo.ArgumentList.Add(_saPassword);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };

        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(process.WaitForExitAsync(), stdOutTask, stdErrTask).ConfigureAwait(false);

        if (stdOutTask.Result is { Length: > 0 })
        {
            _deploymentLog.AddRange(stdOutTask.Result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        }

        if (stdErrTask.Result is { Length: > 0 })
        {
            _deploymentLog.AddRange(stdErrTask.Result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        }

        if (process.ExitCode != 0)
        {
            var message = string.Join(Environment.NewLine, _deploymentLog);
            throw new InvalidOperationException($"deploy-database.ps1 failed with exit code {process.ExitCode}:{Environment.NewLine}{message}");
        }
    }

    private async Task<SqlServerDeploymentBaseline> CaptureBaselineAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    AssemblyCount = (SELECT COUNT(*) FROM sys.assemblies WHERE name = 'SqlClrFunctions'),
    ProcedureCount = (SELECT COUNT(*) FROM sys.objects WHERE type IN ('P','PC','FN','TF','IF') AND is_ms_shipped = 0),
    ServiceQueueCount = (SELECT COUNT(*) FROM sys.service_queues WHERE is_ms_shipped = 0);
";

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("Baseline query returned no rows.");
        }

        var assemblyCount = reader.GetInt32(0);
        var procedureCount = reader.GetInt32(1);
        var queueCount = reader.GetInt32(2);

        return new SqlServerDeploymentBaseline(assemblyCount, procedureCount, queueCount);
    }

    private static string ResolvePowerShellExecutable()
    {
        if (OperatingSystem.IsWindows())
        {
            var pwshPath = TryLocatePwshOnWindows();
            return pwshPath ?? "powershell";
        }

        return "pwsh";
    }

    private static string? TryLocatePwshOnWindows()
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            var candidate = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Hartonomous.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new InvalidOperationException("Unable to locate repository root (Hartonomous.sln not found).");
        }

        return current.FullName;
    }

    private string BuildHartonomousConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_container.ConnectionString)
        {
            InitialCatalog = "Hartonomous",
            UserID = "sa",
            Password = _saPassword,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };

        return builder.ConnectionString;
    }

}

public sealed record SqlServerDeploymentBaseline(int AssemblyCount, int ProcedureCount, int ServiceQueueCount)
{
    public static SqlServerDeploymentBaseline Empty { get; } = new(0, 0, 0);
}
