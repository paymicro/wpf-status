using System.IO;
using System.Text.Json;

namespace WpfStatus
{
    public class ExportNodeLayer
    {
        public string NodeName { get; set; } = string.Empty;

        public string Eligibilities { get; set; } = string.Empty;

        static readonly string LayersPath = "nodeLayers.json";

        public static void Save(List<ExportNodeLayer> layers)
        {
            string json = JsonSerializer.Serialize(layers, Helper.Json.SerializerOptions);
            File.WriteAllText(LayersPath, json);
        }

        public static List<ExportNodeLayer> Load()
        {
            if (File.Exists(LayersPath))
            {
                string json = File.ReadAllText(LayersPath);
                return JsonSerializer.Deserialize<List<ExportNodeLayer>>(json, Helper.Json.SerializerOptions) ?? [];
            }

            return [];
        }
    }
}
