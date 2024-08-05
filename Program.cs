using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Timers;

namespace FileSystemMonitor
{
    class Program
    {
        static Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();
        static string? logFilePath;
        static List<LogEntry> allLogEntries = new List<LogEntry>();
        static System.Timers.Timer? saveTimer;
        static object lockObj = new object();

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: FileSystemMonitor.exe <directory1> <directory2> ... <logFilePath>");
                return;
            }

            logFilePath = args[^1]; // Last argument is the log file path
            var directoriesToMonitor = new List<string>(args[..^1]); // All arguments except the last one

            // Ensure base log directory exists
            string baseLogPath = Path.GetDirectoryName(logFilePath) ?? string.Empty;

            try
            {
                Directory.CreateDirectory(baseLogPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access to the path '{baseLogPath}' is denied. {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the log directory: {ex.Message}");
                return;
            }

            // Initialize FileSystemWatchers for each directory
            foreach (var dir in directoriesToMonitor)
            {
                try
                {
                    var watcher = new FileSystemWatcher
                    {
                        Path = dir,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true,
                        InternalBufferSize = 65536 // 64KB
                    };

                    watcher.Created += (sender, e) => OnChanged(sender, e, dir);
                    watcher.Changed += (sender, e) => OnChanged(sender, e, dir);
                    watcher.Deleted += (sender, e) => OnChanged(sender, e, dir);
                    watcher.Renamed += (sender, e) => OnRenamed(sender, e, dir);

                    watchers[dir] = watcher;
                    Console.WriteLine($"Monitoring changes in {dir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while initializing watcher for directory '{dir}': {ex.Message}");
                }
            }

            // Initialize and start the timer to save logs every minute
            saveTimer = new System.Timers.Timer(60000); // 60,000 milliseconds = 1 minute
            saveTimer.Elapsed += (sender, e) => SaveLogEntries();
            saveTimer.AutoReset = true;
            saveTimer.Enabled = true;

            Console.WriteLine("Press [Enter] to stop monitoring.");
            Console.ReadLine();

            // Save remaining log entries when the application exits
            SaveLogEntries();

            // Dispose of the timer and watchers
            saveTimer.Stop();
            saveTimer.Dispose();
            foreach (var watcher in watchers.Values)
            {
                watcher.Dispose();
            }
        }

        private static void OnChanged(object sender, FileSystemEventArgs e, string directory)
        {
            LogEvent(e.ChangeType.ToString(), e.FullPath, null);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e, string directory)
        {
            LogEvent("Renamed", e.FullPath, e.OldFullPath);
        }

        private static void LogEvent(string changeType, string fullPath, string? oldFullPath)
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ChangeType = changeType,
                FullPath = fullPath,
                OldFullPath = oldFullPath
            };

            lock (lockObj)
            {
                allLogEntries.Add(logEntry);
            }
        }

        private static void SaveLogEntries()
        {
            lock (lockObj)
            {
                if (allLogEntries.Count > 0)
                {
                    string json = JsonSerializer.Serialize(allLogEntries, new JsonSerializerOptions { WriteIndented = true });

                    try
                    {
                        File.WriteAllText(logFilePath, json);
                        Console.WriteLine($"Log entries saved to {logFilePath}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"Access to the path '{logFilePath}' is denied. {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while saving the log entries: {ex.Message}");
                    }

                    allLogEntries.Clear(); // Clear entries after saving
                }
            }
        }
    }

    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string? OldFullPath { get; set; }
    }
}