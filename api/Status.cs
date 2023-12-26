using System.Text.Json.Serialization;

namespace WpfStatus.api
{
    public class Status
    {
        public string ConnectedPeers { get; set; } = string.Empty;

        public bool? IsSynced { get; set; } = null;

        public Layer SyncedLayer { get; set; } = new();

        public Layer TopLayer { get; set; } = new();

        public override string ToString()
        {
            return $"IsSync: {IsSynced==true} | Layer: {SyncedLayer.Number}/{TopLayer.Number}";
        }
    }
}
