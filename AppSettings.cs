using System.IO;
using System.Text.Json;
using WpfStatus.Notification;

namespace WpfStatus
{
    public class AppSettings
    {
        private static readonly string SettingsPath = "settings.json";

        public List<NodeSetting> Nodes { get; set; } = [];

        public string Coinbase { get; set; } = string.Empty;

        public List<TableHeader> TableHeaders { get; set; } = [];

        public string AppTitle { get; set; } = "Status";

        public bool IsTimerEnabled { get; set; } = true;

        public NotificationSettings NotificationSettings { get; set; } = new();

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
