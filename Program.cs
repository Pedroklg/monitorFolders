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
        static string baseLogPath = @"C:\Users\pedro_silva\Desktop\LogsPastaP"; // Path for the log file
        static Dictionary<string, List<LogEntry>> directoryLogs = new Dictionary<string, List<LogEntry>>();
        static System.Timers.Timer? saveTimer;
        static object lockObj = new object();

        static void Main(string[] args)
        {
            // List of directories to monitor
            List<string> directoriesToMonitor = new List<string>
            {
                @"\\mm-dfs\CDAM"
                // Add more directories as needed
            };

            // Ensure base log directory exists
            Directory.CreateDirectory(baseLogPath);

            // Initialize FileSystemWatchers for each directory
            foreach (var dir in directoriesToMonitor)
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
                directoryLogs[dir] = new List<LogEntry>();

                Console.WriteLine($"Monitoring changes in {dir}");
            }

            // Initialize and start the timer to save logs every minute
            saveTimer = new System.Timers.Timer(60000); //  1 minute
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
            LogEvent(e.ChangeType.ToString(), e.FullPath, null, directory);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e, string directory)
        {
            LogEvent("Renamed", e.FullPath, e.OldFullPath, directory);
        }

        private static void LogEvent(string changeType, string fullPath, string? oldFullPath, string directory)
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
                if (!directoryLogs.ContainsKey(directory))
                {
                    directoryLogs[directory] = new List<LogEntry>();
                }
                directoryLogs[directory].Add(logEntry);
            }
        }

        private static void SaveLogEntries()
        {
            lock (lockObj)
            {
                foreach (var dir in directoryLogs.Keys)
                {
                    var logEntries = directoryLogs[dir];
                    if (logEntries.Count > 0)
                    {
                        string logDir = Path.Combine(baseLogPath, Path.GetFileName(dir));
                        Directory.CreateDirectory(logDir);

                        string logFilePath = Path.Combine(logDir, "events.json");

                        List<LogEntry> existingLogEntries = new List<LogEntry>();
                        if (File.Exists(logFilePath))
                        {
                            string existingJson = File.ReadAllText(logFilePath);
                            existingLogEntries = JsonSerializer.Deserialize<List<LogEntry>>(existingJson) ?? new List<LogEntry>();
                        }

                        existingLogEntries.AddRange(logEntries);

                        string json = JsonSerializer.Serialize(existingLogEntries, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(logFilePath, json);
                        directoryLogs[dir].Clear(); // Clear entries after saving
                    }
                }
            }
        }
    }

    public class LogEntry
    {
        public required string Timestamp { get; set; }
        public required string ChangeType { get; set; }
        public required string FullPath { get; set; }
        public string? OldFullPath { get; set; }
    }
}
