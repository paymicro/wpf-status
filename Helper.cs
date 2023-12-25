using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace WpfStatus
{
    public class Helper
    {
        string RPC = "grpcurl.exe";

        public async Task<string> CallGPRC(string host, int port, string api, int maxTime)
        {
            if (port == 0)
            {
                return string.Empty;
            }
            var output = new StringBuilder();
            var param = $"--plaintext -max-time {maxTime} {host}:{port} {api}";
            var process = new Process
            {
                StartInfo = new()
                {
                    FileName = RPC,
                    Arguments = param,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.OutputDataReceived += new((s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Append(e.Data + Environment.NewLine);
                }
            });
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            return output.ToString();
        }

        public static partial class Json
        {
            public static JsonSerializerOptions SerializerOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true                
            };

            public static T? Deserialize<T>(string json, T anonymousTypeObject)
            {
                return JsonSerializer.Deserialize<T>(json, SerializerOptions);
            }
        }

        public static string TimeToDaysString(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 1)
            {
                return timeSpan.ToString(@"d\d\ hh\h");
            }
            return timeSpan.ToString(@"hh\h\ mm\m");
        }

        readonly static int layerDurationMinutes = 5;
        readonly static int markLayerId = 20500;
        readonly static DateTime markLayerTime = DateTime.Parse("2023-09-23T15:20:00+0300");

        public static int GetLayerByTime(DateTime time)
        {
            var layersGapMinutes = (time - markLayerTime).TotalMinutes;
            var layersDelta = layersGapMinutes / layerDurationMinutes;
            return (int)(markLayerId + layersDelta);
        }

        public static DateTime GetTimeByLayer(int layer)
        {
            return markLayerTime.AddMinutes((layer - markLayerId) * layerDurationMinutes);
        }
    }
}
