using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("RobocopyGui.Tests")]

namespace RobocopyGui
{
    public class RobocopyProgressEventArgs : EventArgs
    {
        public int Percentage { get; }
        public string CurrentFile { get; }

        public RobocopyProgressEventArgs(int percentage, string currentFile)
        {
            Percentage = percentage;
            CurrentFile = currentFile;
        }
    }

    public class RobocopyOverallProgressEventArgs : EventArgs
    {
        public int TotalFiles { get; }
        public int CopiedFiles { get; }
        public int OverallPercentage { get; }

        public RobocopyOverallProgressEventArgs(int totalFiles, int copiedFiles, int overallPercentage)
        {
            TotalFiles = totalFiles;
            CopiedFiles = copiedFiles;
            OverallPercentage = overallPercentage;
        }
    }

    public class RobocopyOutputEventArgs : EventArgs
    {
        public string Line { get; }

        public RobocopyOutputEventArgs(string line)
        {
            Line = line;
        }
    }

    public class RobocopyRunner
    {
        static RobocopyRunner()
        {
            // Register encoding provider for legacy OEM/ANSI encodings like code page 850
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public event EventHandler<RobocopyProgressEventArgs>? ProgressChanged;
        public event EventHandler<RobocopyOverallProgressEventArgs>? OverallProgressChanged;
        public event EventHandler<RobocopyOutputEventArgs>? OutputReceived;
        public event EventHandler<string>? StatusChanged;

        private Process? _process;
        private CancellationTokenSource? _cts;
        private string _currentFile = string.Empty;

        private int _totalFiles = 0;
        private int _copiedFiles = 0;

        // Regex to match percentages like "15.3%" or "10%"
        private static readonly Regex ProgressRegex = new Regex(@"(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);
        // Regex to match summary lines like "   Files :         3         3         0         0         0         0"
        private static readonly Regex SummaryFilesRegex = new Regex(@"^\s*Files\s*:\s*(\d+)\s*(\d+)", RegexOptions.Compiled);

        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, $"robocopy_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        private void WriteLog(string message)
        {
            try
            {
                if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogFilePath, $"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        public async Task<int> RunAsync(
            string source,
            string destination,
            AppConfig config,
            string[] excludedDirs,
            string[] excludedFiles)
        {
            _cts = new CancellationTokenSource();
            _totalFiles = CountFiles(source, config.FileFilter, excludedDirs);
            _copiedFiles = 0;
            UpdateOverallProgress();

            // Build Robocopy command line arguments
            var argsBuilder = new StringBuilder();

            argsBuilder.Append($"\"{source}\" \"{destination}\"");
            if (!string.IsNullOrWhiteSpace(config.FileFilter))
            {
                argsBuilder.Append($" {config.FileFilter}");
            }

            argsBuilder.Append(" /E");
            if (config.UseUnbufferedIo) argsBuilder.Append(" /J");
            if (config.UseRestartableMode) argsBuilder.Append(" /Z");
            argsBuilder.Append($" /R:{config.Retries}");
            argsBuilder.Append($" /W:{config.WaitTime}");

            if (excludedDirs.Length > 0)
            {
                argsBuilder.Append(" /XD");
                foreach (var dir in excludedDirs)
                {
                    argsBuilder.Append($" \"{dir}\"");
                }
            }
            if (excludedFiles.Length > 0)
            {
                argsBuilder.Append(" /XF");
                foreach (var file in excludedFiles)
                {
                    argsBuilder.Append($" \"{file}\"");
                }
            }

            argsBuilder.Append(" /V /BYTES");

            string args = argsBuilder.ToString();
            WriteLog($"Starting Robocopy (Target: {_totalFiles} files): robocopy.exe {args}");
            StatusChanged?.Invoke(this, "Starting Robocopy process...");

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(850)
                }
            };
            // ... (rest of method continues)

            // Helper to count files
            static int CountFiles(string path, string filter, string[] excludedDirs)
            {
                int count = 0;
                try
                {
                    var dirInfo = new DirectoryInfo(path);
                    foreach (var file in dirInfo.EnumerateFiles(filter, SearchOption.AllDirectories))
                    {
                        // Check if file is inside any excluded directory
                        bool isExcluded = false;
                        foreach (var excludedDir in excludedDirs)
                        {
                            if (file.FullName.StartsWith(excludedDir, StringComparison.OrdinalIgnoreCase))
                            {
                                isExcluded = true;
                                break;
                            }
                        }
                        if (!isExcluded) count++;
                    }
                }
                catch { /* Ignore access errors */ }
                return count;
            }


            try
            {
                if (!_process.Start())
                {
                    WriteLog("Failed to start robocopy.exe");
                    throw new InvalidOperationException("Failed to start robocopy.exe");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Error starting process: {ex.Message}");
                StatusChanged?.Invoke(this, $"Error: {ex.Message}");
                return -1;
            }

            // Run the output parsing in a background task
            Task outputTask = ReadStreamAsync(_process.StandardOutput, _cts.Token);
            Task errorTask = ReadStreamAsync(_process.StandardError, _cts.Token);

            await Task.WhenAll(outputTask, errorTask);

            await _process.WaitForExitAsync();
            int exitCode = _process.ExitCode;

            string exitMessage = MapExitCode(exitCode);
            StatusChanged?.Invoke(this, $"Completed: {exitMessage} (Code {exitCode})");

            return exitCode;
        }

        public void Stop()
        {
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    StatusChanged?.Invoke(this, "Stopping Robocopy process...");
                    _cts?.Cancel();
                    _process.Kill(true); // Kill entire process tree
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke(this, $"Error during stop: {ex.Message}");
                }
            }
        }

        private async Task ReadStreamAsync(StreamReader reader, CancellationToken token)
        {
            char[] buffer = new char[4096];
            StringBuilder lineBuilder = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await reader.ReadAsync(buffer, token);
                    if (bytesRead == 0) break;

                    for (int i = 0; i < bytesRead; i++)
                    {
                        char c = buffer[i];

                        // Split on carriage return or line feed.
                        // Robocopy uses \r to overwrite percentage progress on the same line.
                        if (c == '\r' || c == '\n')
                        {
                            if (lineBuilder.Length > 0)
                            {
                                string line = lineBuilder.ToString();
                                WriteLog(line); // Log every line
                                ParseLine(line);
                                lineBuilder.Clear();
                            }
                        }
                        else
                        {
                            lineBuilder.Append(c);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("Robocopy process was cancelled");
                // Task was cancelled, exit cleanly
            }
            catch (Exception ex)
            {
                WriteLog($"Stream read error: {ex.Message}");
                OutputReceived?.Invoke(this, new RobocopyOutputEventArgs($"Stream read error: {ex.Message}"));
            }
        }

        internal void ParseLine(string line)
        {
            string cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine)) return;

            // Emit raw output to the log panel
            OutputReceived?.Invoke(this, new RobocopyOutputEventArgs(line));

            // Check if it's a file line (e.g. "New File    15  file1.txt" or "New File   15  file1.txt100%")
            if (cleanLine.Contains("New File") || cleanLine.Contains("New Dir"))
            {
                // Remove trailing percentage if attached
                string lineWithoutPct = ProgressRegex.Replace(cleanLine, "").Trim();
                var parts = lineWithoutPct.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    string candidate = parts[^1];
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        _currentFile = candidate;
                        ProgressChanged?.Invoke(this, new RobocopyProgressEventArgs(0, _currentFile));
                    }
                }
            }

            // Check if there is a percentage update on this line
            var match = ProgressRegex.Match(cleanLine);
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double percentage))
                {
                    int pct = (int)Math.Round(percentage);
                    ProgressChanged?.Invoke(this, new RobocopyProgressEventArgs(pct, _currentFile));
                }
            }

            // Parse summary headers or track copied files count
            if (cleanLine.Contains("Files :"))
            {
                var summaryMatch = SummaryFilesRegex.Match(cleanLine);
                if (summaryMatch.Success && int.TryParse(summaryMatch.Groups[1].Value, out int totalFiles))
                {
                    _totalFiles = totalFiles;
                    UpdateOverallProgress();
                }
            }
            else if (cleanLine.Contains("New File"))
            {
                _copiedFiles++;
                UpdateOverallProgress();
            }
        }

        private void UpdateOverallProgress()
        {
            if (_totalFiles > 0)
            {
                int overallPct = (_copiedFiles * 100) / _totalFiles;
                OverallProgressChanged?.Invoke(this, new RobocopyOverallProgressEventArgs(_totalFiles, _copiedFiles, Math.Clamp(overallPct, 0, 100)));
            }
        }

        public static string MapExitCode(int code)
        {
            // Robocopy exit codes are bitmasks:
            // 0: No files copied, no errors.
            // 1: One or more files copied successfully.
            // 2: Extra files/dirs detected.
            // 3: Files copied + Extra files/dirs detected.
            // 4: Mismatched files/dirs detected.
            // 5: Files copied + Mismatched files/dirs.
            // 6: Extra + Mismatched files/dirs.
            // 7: Files copied + Extra + Mismatched.
            // 8: Some files failed to copy.
            // 16: Fatal error (no files copied).

            if (code == 0) return "No files copied (source and destination identical)";
            if (code == 1) return "Success (Files copied successfully)";
            if (code == 2) return "Success (Extra files detected in destination)";
            if (code == 3) return "Success (Files copied, extra files detected)";
            if (code == 4) return "Completed with warnings (Mismatched files/directories detected)";
            if (code == 5) return "Completed with warnings (Files copied, mismatched items found)";
            if (code == 6) return "Completed with warnings (Extra and mismatched items found)";
            if (code == 7) return "Completed with warnings (Files copied, extra and mismatched items found)";
            if (code >= 8 && code < 16) return "Error (Some files failed to copy; check log)";
            if (code >= 16) return "Fatal Error (No files were copied; serious network or path error)";

            return $"Unknown Exit Code ({code})";
        }
    }
}
