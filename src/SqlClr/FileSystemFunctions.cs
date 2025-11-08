using System;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    /// <summary>
    /// File system operations for autonomous deployment and FILESTREAM migration
    /// Requires UNSAFE permission set for file I/O
    /// </summary>
    public class FileSystemFunctions
    {
        /// <summary>
        /// Write bytes to a file path on disk
        /// Used for autonomous deployment (Phase 4: write generated code)
        /// </summary>
        /// <param name="filePath">Absolute path to file (e.g., C:\Code\generated.sql)</param>
        /// <param name="content">Binary content to write</param>
        /// <returns>Number of bytes written, or -1 on error</returns>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlInt64 WriteFileBytes(SqlString filePath, SqlBytes content)
        {
            if (filePath.IsNull || content.IsNull)
                return SqlInt64.Null;

            try
            {
                string path = filePath.Value;
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write bytes to file
                byte[] bytes = content.Value;
                File.WriteAllBytes(path, bytes);
                
                return new SqlInt64(bytes.Length);
            }
            catch (Exception ex)
            {
                // Return -1 on error; caller should check
                SqlContext.Pipe.Send($"Error writing file: {ex.Message}");
                return new SqlInt64(-1);
            }
        }

        /// <summary>
        /// Write UTF-8 text to a file path on disk
        /// Convenience wrapper for text-based code generation
        /// </summary>
        /// <param name="filePath">Absolute path to file</param>
        /// <param name="content">Text content to write</param>
        /// <returns>Number of bytes written, or -1 on error</returns>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlInt64 WriteFileText(SqlString filePath, SqlString content)
        {
            if (filePath.IsNull || content.IsNull)
                return SqlInt64.Null;

            try
            {
                string path = filePath.Value;
                string text = content.Value;

                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write UTF-8 text to file
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                File.WriteAllBytes(path, bytes);

                return new SqlInt64(bytes.Length);
            }
            catch
            {
                return new SqlInt64(-1);
            }
        }

        /// <summary>
        /// Read bytes from a file path on disk
        /// Used for FILESTREAM migration (read PayloadLocator files)
        /// </summary>
        /// <param name="filePath">Absolute path to file</param>
        /// <returns>File contents as VARBINARY(MAX)</returns>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlBytes ReadFileBytes(SqlString filePath)
        {
            if (filePath.IsNull)
                return SqlBytes.Null;

            try
            {
                string path = filePath.Value;

                if (!File.Exists(path))
                    return SqlBytes.Null;

                byte[] bytes = File.ReadAllBytes(path);
                return new SqlBytes(bytes);
            }
            catch
            {
                return SqlBytes.Null;
            }
        }

        /// <summary>
        /// Read UTF-8 text from a file path on disk
        /// Convenience wrapper for reading text files
        /// </summary>
        /// <param name="filePath">Absolute path to file</param>
        /// <returns>File contents as NVARCHAR(MAX)</returns>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlString ReadFileText(SqlString filePath)
        {
            if (filePath.IsNull)
                return SqlString.Null;

            try
            {
                string path = filePath.Value;

                if (!File.Exists(path))
                {
                    SqlContext.Pipe.Send($"File not found: {path}");
                    return SqlString.Null;
                }

                string text = File.ReadAllText(path, Encoding.UTF8);
                return new SqlString(text);
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"Error reading file: {ex.Message}");
                return SqlString.Null;
            }
        }

        /// <summary>
        /// Execute shell command and return output
        /// Used for git operations (add, commit, push) in autonomous deployment
        /// SECURITY: Uses ArgumentList to prevent command injection
        /// </summary>
        /// <param name="executable">Executable name (e.g., "git", "dotnet")</param>
        /// <param name="arguments">Arguments as JSON array (e.g., "[\"add\", \".\"]")</param>
        /// <param name="workingDirectory">Working directory for command execution</param>
        /// <param name="timeoutSeconds">Command timeout in seconds (default 30)</param>
        /// <returns>Table of output lines from command</returns>
        [SqlFunction(
            IsDeterministic = false,
            IsPrecise = true,
            DataAccess = DataAccessKind.None,
            TableDefinition = "OutputLine NVARCHAR(MAX), IsError BIT",
            FillRowMethodName = "FillShellOutputRow")]
        public static System.Collections.IEnumerable ExecuteShellCommand(
            SqlString executable,
            SqlString arguments,
            SqlString workingDirectory,
            SqlInt32 timeoutSeconds)
        {
            var results = new System.Collections.Generic.List<ShellOutputRow>();

            if (executable.IsNull)
            {
                results.Add(new ShellOutputRow { OutputLine = "Error: Executable is null", IsError = true });
                return results;
            }

            try
            {
                string exe = executable.Value;
                string workDir = workingDirectory.IsNull ? Environment.CurrentDirectory : workingDirectory.Value;
                int timeout = timeoutSeconds.IsNull ? 30 : timeoutSeconds.Value;

                // SECURITY FIX: Use ProcessStartInfo.ArgumentList to prevent injection
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = workDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Parse arguments from JSON array to prevent command injection
                if (!arguments.IsNull && !string.IsNullOrWhiteSpace(arguments.Value))
                {
                    try
                    {
                        // Parse JSON array: ["arg1", "arg2", "arg3"]
                        var argString = arguments.Value.Trim();
                        if (argString.StartsWith("[") && argString.EndsWith("]"))
                        {
                            // Remove brackets and split by comma
                            var content = argString.Substring(1, argString.Length - 2);
                            var parts = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var part in parts)
                            {
                                var trimmed = part.Trim();
                                // Remove quotes
                                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                                {
                                    trimmed = trimmed.Substring(1, trimmed.Length - 2);
                                }
                                // Add to Arguments string (escaped for .NET Framework 4.8.1)
                                processInfo.Arguments += " \"" + trimmed.Replace("\"", "\\\"") + "\"";
                            }
                        }
                        else
                        {
                            // Simple argument (no JSON)
                            processInfo.Arguments += " \"" + arguments.Value.Replace("\"", "\\\"") + "\"";
                        }
                    }
                    catch
                    {
                        // If JSON parsing fails, treat as single argument
                        processInfo.Arguments += " \"" + arguments.Value.Replace("\"", "\\\"") + "\"";
                    }
                }

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        results.Add(new ShellOutputRow { OutputLine = "Error: Failed to start process", IsError = true });
                        return results;
                    }

                    // Read output asynchronously
                    var stdOut = new StringBuilder();
                    var stdErr = new StringBuilder();

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                            stdOut.AppendLine(args.Data);
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                            stdErr.AppendLine(args.Data);
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool exited = process.WaitForExit(timeout * 1000);

                    if (!exited)
                    {
                        process.Kill();
                        results.Add(new ShellOutputRow { OutputLine = $"Error: Command timed out after {timeout} seconds", IsError = true });
                        return results;
                    }

                    // Add standard output
                    if (stdOut.Length > 0)
                    {
                        foreach (string line in stdOut.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            results.Add(new ShellOutputRow { OutputLine = line, IsError = false });
                        }
                    }

                    // Add standard error
                    if (stdErr.Length > 0)
                    {
                        foreach (string line in stdErr.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            results.Add(new ShellOutputRow { OutputLine = line, IsError = true });
                        }
                    }

                    // Add exit code
                    if (process.ExitCode != 0)
                    {
                        results.Add(new ShellOutputRow { OutputLine = $"Exit Code: {process.ExitCode}", IsError = true });
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new ShellOutputRow { OutputLine = $"Error executing command: {ex.Message}", IsError = true });
            }

            return results;
        }

        /// <summary>
        /// Fill row method for ExecuteShellCommand table-valued function
        /// </summary>
        public static void FillShellOutputRow(
            object obj,
            out SqlString outputLine,
            out SqlBoolean isError)
        {
            var row = (ShellOutputRow)obj;
            outputLine = new SqlString(row.OutputLine);
            isError = new SqlBoolean(row.IsError);
        }

        /// <summary>
        /// Check if a file exists on disk
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlBoolean FileExists(SqlString filePath)
        {
            if (filePath.IsNull)
                return SqlBoolean.Null;

            try
            {
                return new SqlBoolean(File.Exists(filePath.Value));
            }
            catch
            {
                return SqlBoolean.False;
            }
        }

        /// <summary>
        /// Check if a directory exists on disk
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlBoolean DirectoryExists(SqlString directoryPath)
        {
            if (directoryPath.IsNull)
                return SqlBoolean.Null;

            try
            {
                return new SqlBoolean(Directory.Exists(directoryPath.Value));
            }
            catch
            {
                return SqlBoolean.False;
            }
        }

        /// <summary>
        /// Delete a file from disk
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        public static SqlBoolean DeleteFile(SqlString filePath)
        {
            if (filePath.IsNull)
                return SqlBoolean.Null;

            try
            {
                if (File.Exists(filePath.Value))
                {
                    File.Delete(filePath.Value);
                    return SqlBoolean.True;
                }
                return SqlBoolean.False;
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"Error deleting file: {ex.Message}");
                return SqlBoolean.False;
            }
        }

        /// <summary>
        /// Helper class for shell command output
        /// </summary>
        private class ShellOutputRow
        {
            public string OutputLine { get; set; }
            public bool IsError { get; set; }
        }
    }
}
