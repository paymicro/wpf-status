namespace WpfStatus.api
{
    public class Event
    {
        public string Timestamp { get; set; } = string.Empty;

        public string Help { get; set; } = string.Empty;

        public Eligibility? Eligibilities { get; set; } = null;

        public string Detail
        {
            get
            {
                if (Eligibilities !=  null)
                {
                    var eli = string.Join(", ", Eligibilities.Eligibilities.Select(e => e.Layer));

                    return $"Eligibilities{Environment.NewLine}" +
                        $"ATX: {Eligibilities.Atx}{Environment.NewLine}" +
                        $"Beacon: {Eligibilities.Beacon}{Environment.NewLine}" +
                        $"ActiveSetSize: {Eligibilities.ActiveSetSize}{Environment.NewLine}" +
                        $"Rewards: {eli}";
                }

                return string.Empty;
            }
        }
    }
}
