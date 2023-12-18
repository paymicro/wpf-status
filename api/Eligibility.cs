namespace WpfStatus.api
{
    public class Eligibility
    {
        public int Epoch { get; set; } = 0;

        public string Beacon { get; set; } = string.Empty;

        public string Atx { get; set; } = string.Empty;

        public int ActiveSetSize { get; set; } = 0;

        public Eligibilities[] Eligibilities { get; set; } = [];
    }
}
