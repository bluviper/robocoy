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
            return Load(out _);
        }

        public static AppConfig Load(out string? error)
        {
            error = null;
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
                error = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
            return new AppConfig();
        }

        public bool Save()
        {
            return Save(out _);
        }

        public bool Save(out string? error)
        {
            error = null;
            try
            {
                string? dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
                return false;
            }
        }
    }
}
