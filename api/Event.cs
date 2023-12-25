namespace WpfStatus.api
{
    public class Event
    {
        public string Timestamp { get; set; } = string.Empty;

        public string Help { get; set; } = string.Empty;

        public EligibilityEvent? Eligibilities { get; set; } = null;

        public ProposalEvent? Proposal { get; set; } = null;

        public BeaconEvent? Beacon { get; set; } = null;

        public string Detail
        {
            get
            {
                if (Eligibilities !=  null)
                {
                    var eli = string.Join(", ", Eligibilities.Eligibilities.Select(e => e.Layer));

                    return $"Eligibilities{Environment.NewLine}" +
                        $"ATX: {Eligibilities.Atx}{Environment.NewLine}" +
                        $"Epoch: {Eligibilities.Epoch} {Environment.NewLine}" +
                        $"Beacon: {Eligibilities.Beacon}{Environment.NewLine}" +
                        $"ActiveSetSize: {Eligibilities.ActiveSetSize}{Environment.NewLine}" +
                        $"Rewards: {eli}";
                }
                else if (Proposal != null)
                {
                    return $"Layer: {Proposal.Layer}{Environment.NewLine}" +
                        $"Proposal: {Proposal.Proposal}";
                }
                else if (Beacon != null)
                {
                    return $"Epoch: {Beacon.Epoch} {Environment.NewLine}" +
                        $"Beacon: {Beacon.Beacon}";
                }

                return string.Empty;
            }
        }
    }
}
