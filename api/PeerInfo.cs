namespace WpfStatus.api
{
    public class PeerInfo
    {
        public string Id { get; set; } = string.Empty;

        public List<Connection> Connections { get; set; } = [];

        // public List<Tag> Tags { get; set; } = [];
    }
}
