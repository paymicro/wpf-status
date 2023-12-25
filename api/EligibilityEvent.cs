namespace WpfStatus.api
{
    public class EligibilityEvent: BeaconEvent
    {
        public string Atx { get; set; } = string.Empty;

        public int ActiveSetSize { get; set; } = 0;

        public Eligibilities[] Eligibilities { get; set; } = [];
    }
}
