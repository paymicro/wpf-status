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
    }
}
