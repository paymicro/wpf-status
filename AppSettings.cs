using System.IO;
using System.Text.Json;

namespace WpfStatus
{
    public class AppSettings
    {
        private static string SettingsPath = "settings.json";

        public List<NodeSetting> Nodes { get; set; }

        public List<TableHeader> TableHeaders { get; set; }
        public string AppTitle { get; set; }
        public bool IsTimerEnabled { get; set; }

        public AppSettings()
        {
            Nodes = new();
            AppTitle = "Status";
            IsTimerEnabled = true;
        }

        public static void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, Helper.Json.SerializerOptions);
            File.WriteAllText(SettingsPath, json);
        }

        public static AppSettings LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, Helper.Json.SerializerOptions) ?? new();
            }

            return new();
        }
    }
}
