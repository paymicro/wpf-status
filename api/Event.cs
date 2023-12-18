namespace WpfStatus.api
{
    public class Event
    {
        public string Timestamp { get; set; } = string.Empty;

        public string Help { get; set; } = string.Empty;

        public Eligibility? Eligibilities { get; set; } = null;
    }
}
