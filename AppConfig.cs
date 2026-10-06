using System;
using System.IO;
using System.Text.Json;

namespace RobocopyGui
{
    public class AppConfig
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public bool UseUnbufferedIo { get; set; } = true; // /J
        public bool UseRestartableMode { get; set; } = true; // /Z
        public int Retries { get; set; } = 3; // /R:3
        public int WaitTime { get; set; } = 2; // /W:2
        public string FileFilter { get; set; } = "*.*";

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                // Simple fail-safe default, we can log to a file or Console
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }
    }
}
