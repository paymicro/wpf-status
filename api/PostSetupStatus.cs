using System.Text.Json.Serialization;

namespace WpfStatus.api
{
    public class PostSetupStatus
    {
        public string State { get; set; } = string.Empty;

        public string NumLabelsWritten { get; set; } = string.Empty;

        public PostSetupStatusOpts Opts { get; set; } = new();

        public string SizeInTib => (Opts.NumUnits * 64 * 0.001f).ToString("0.00 Tib");
    }

    public class PostSetupStatusOpts
    {
        public string DataDir { get; set; } = string.Empty;

        public long NumUnits { get; set; } = 0;

        public string MaxFileSize { get; set; } = string.Empty;

        public long ProviderId { get; set; } = 0;
    }
}
