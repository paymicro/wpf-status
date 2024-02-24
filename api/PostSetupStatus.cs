namespace WpfStatus.api
{
    public class PostSetupStatus
    {
        public string State { get; set; } = string.Empty;

        public string NumLabelsWritten { get; set; } = string.Empty;

        public PostSetupStatusOpts Opts { get; set; } = new();

        public string SizeInTib
        {
            get
            {
                if (Opts.NumUnits == 0)
                {
                    return string.Empty;
                }

                var tib = (Opts.NumUnits * 64 * 0.001f).ToString("0.00 Tib");
                if (State == "STATE_IN_PROGRESS" && long.TryParse(NumLabelsWritten, out long labelsWriten))
                {
                    var persent = Math.Round((labelsWriten / 1024d / 1024 / 1024 * 16) / (Opts.NumUnits * 0.64), 1);
                    return $"{persent}%   {tib}";
                }
                return tib;
            }
        }
    }

    public class PostSetupStatusOpts
    {
        public string DataDir { get; set; } = string.Empty;

        public long NumUnits { get; set; } = 0;

        public string MaxFileSize { get; set; } = string.Empty;

        public long ProviderId { get; set; } = 0;
    }
}
