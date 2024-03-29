﻿using System.Text.Json.Serialization;

namespace WpfStatus.api
{
    public class Status
    {
        public string ConnectedPeers { get; set; } = string.Empty;

        public bool IsSynced { get; set; } = false;

        public Layer SyncedLayer { get; set; } = new();

        public Layer TopLayer { get; set; } = new();

        public override string ToString()
        {
            return $"IsSync: {IsSynced} | Layer: {SyncedLayer.Number}/{TopLayer.Number}";
        }
    }
}
