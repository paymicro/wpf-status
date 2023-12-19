namespace WpfStatus.api
{
    public class Connection
    {
        public string Address { get; set; } = string.Empty;

        public string Uptime { get; set; } = string.Empty;

        public long UptimeSec
        {
            get
            {
                if (float.TryParse(Uptime.TrimEnd('s'), out float result))
                {
                    return (long)result;
                }
                return 0;
            }
        }

        public bool Outbound { get; set; } = false;
    }
}
