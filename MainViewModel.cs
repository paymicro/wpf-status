using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using WpfStatus.api;
using static WpfStatus.Helper;

namespace WpfStatus
{
    public class MainViewModel : INotifyPropertyChanged
    {
        readonly AppSettings appSettings;
        readonly List<ExportNodeLayer> nodeLayers;

        public MainViewModel(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            foreach (var node in appSettings.Nodes)
            {
                Nodes.Add(new Node
                {
                    Name = node.Name,
                    Port = node.Port,
                    Host = node.Host,
                    AdminPort = node.AdminPort,
                });
            }
            MainWindowTitle = appSettings.AppTitle;
            IsAutoUpdate = appSettings.IsTimerEnabled;
            IsEnabledNotifications = appSettings.NotificationSettings.Enabled;
            nodeLayers = ExportNodeLayer.Load();
            _ = UpdateInfo();
        }

        int _progressValue;

        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        public string MainWindowTitle { get; set; }

        public bool IsAutoUpdate { get; set; }

        public bool IsEnabledNotifications { get; set; }

        public ObservableCollection<Node> Nodes { get; set; } = [];

        public ObservableCollection<PeerInfo> PeerInfos { get; set; } = [];

        public ObservableCollection<Event> Events { get; set; } = [];

        public ObservableCollection<TimeEvent> TimeEvents { get; set; } = [];

        private Node? _selectedNode;

        public Node? SelectedNode
        {
            get => _selectedNode;
            set
            {
                _selectedNode = value;
                if (value != null)
                {
                    UpdatePeerInfosFrom(value);
                }
                OnPropertyChanged(nameof(SelectedNode));
            }
        }

        DateTime lastUpdateAll = DateTime.MinValue;

        public async Task UpdateAllNodes()
        {
            foreach (var node in Nodes)
            {
                if ((DateTime.Now - lastUpdateAll).TotalMinutes > 5)
                {
                    lastUpdateAll = DateTime.Now;
                    node.IsUpdateEvents = true;
                    node.IsUpdatePostSetup = true;
                }

                await node.Update();
            }

            if (_selectedNode != null)
            {
                UpdatePeerInfosFrom(_selectedNode);
            }
            await UpdateInfo();
            ExportNodeLayer.Save(nodeLayers);
        }

        List<RewardEntity> RewardsList = [];

        DateTime lastGettingRewards = DateTime.MinValue;

        void UpdateSavedLayers(string nodeName, string layerNums)
        {
            var savedNode = nodeLayers.FirstOrDefault(n => n.NodeName == nodeName);
            if (savedNode == default)
            {
                nodeLayers.Add(
                    new ExportNodeLayer()
                    {
                        NodeName = nodeName,
                        Eligibilities = layerNums
                    });
            }
            else
            {
                savedNode.Eligibilities = layerNums;
            }
        }

        async Task UpdateInfo(bool updateRewards = true)
        {
            var markLayerTime = DateTime.Parse("2023-09-23T15:20:00+0300");
            var markEpochNumber = 6;
            var markEpochBegin = DateTime.Parse("2023-10-06 11:00:00+0300");
            var eDurationMs = TimeSpan.FromDays(14);  // 2 weeks
            var official12hOffset = TimeSpan.FromDays(9.5); // -228h
            var official12hOffset2 = TimeSpan.FromDays(10); // +12h
            var ePassedNum = (int)((DateTime.Now - markEpochBegin) / eDurationMs);
            var eCurrentNum = markEpochNumber + ePassedNum;
            var eCurrentBegin = markEpochBegin.Add(eDurationMs * (eCurrentNum - markEpochNumber));
            var beginEpohLayer = GetLayerByTime(eCurrentBegin);
            var currentLayer = GetLayerByTime(DateTime.Now);

            var events = new List<TimeEvent>
            {
                new() { DateTime = eCurrentBegin, Desc = $"Epoch {eCurrentNum - 1} End" },
                new() { DateTime = eCurrentBegin.Add(official12hOffset), Desc = $"PoST {eCurrentNum - 1} Begin"},
                new() { DateTime = eCurrentBegin.Add(official12hOffset2), Desc = $"PoST {eCurrentNum - 1} 12h End" },
                new() { DateTime = eCurrentBegin.Add(eDurationMs), Desc = $"PoST {eCurrentNum} 108h End" },
                new() { DateTime = DateTime.Now, Desc = "We are here", EventType = 1 },
            };

            if (updateRewards &&
                !string.IsNullOrWhiteSpace(appSettings.Coinbase) &&
                appSettings.Coinbase.StartsWith("sm1qqqqqq") &&
                (DateTime.Now - lastGettingRewards).TotalMinutes > 2)
            {
                using var client = new HttpClient();
                var result = await client.GetStringAsync($"https://mainnet-explorer-api.spacemesh.network/accounts/{appSettings.Coinbase}/rewards?page=1&pagesize=200");
                var rewards = Json.Deserialize(result, new { Data = new List<RewardEntity>(), Paginatiaon = new object() })?.Data ?? [];
                lastGettingRewards = DateTime.Now;
                RewardsList = rewards.Where(r => r.Layer > beginEpohLayer).ToList();
            }

            foreach (var node in Nodes)
            {
                var eli = node.Events.Select(e => e.Eligibilities).Where(e => e != null).FirstOrDefault();
                List<TimeEvent> preparedEvents = [];
                if (eli != null)
                {
                    UpdateSavedLayers(node.Name, string.Join(',', eli.Eligibilities.Select(r => r.Layer)));
                    preparedEvents = eli.Eligibilities.Select(r => new TimeEvent()
                    {
                        Layer = r.Layer,
                        Desc = node.Name,
                        EventType = 2
                    }).ToList();
                }
                else
                {
                    var saved = nodeLayers.FirstOrDefault(n => n.NodeName == node.Name);
                    if (saved != default)
                    {
                        preparedEvents = saved.Eligibilities.Split(',').Select(l => new TimeEvent()
                        {
                            Layer = Convert.ToInt32(l),
                            Desc = node.Name,
                            EventType = 2
                        }).ToList();
                    }
                }
                foreach (var e in preparedEvents.Where(prep => prep.Layer <= currentLayer))
                {
                    var reward = RewardsList.FirstOrDefault(r => r.Layer == e.Layer);
                    if (reward != null)
                    {
                        e.RewardStr = $"+ {Math.Round(reward.Total / 1000_000_000d, 3)}";
                    }
                    else
                    {
                        e.RewardStr = "❌";
                    }
                    e.RewardVisible = Visibility.Visible;
                }
                events.AddRange(preparedEvents);
            }

            var eventsStr = events.Select(e =>
                (e.DateTime - DateTime.Now).TotalHours.ToString("0.0") + "h" +
                Environment.NewLine + e.Desc);

            var selectedEvent = TimeEvents.FirstOrDefault(e => e.IsSelected);
            var newSelectedEvent = events.FirstOrDefault(e => e.Layer == selectedEvent?.Layer);
            if (newSelectedEvent != null)
            {
                newSelectedEvent.IsSelected = true;
            }

            events.Sort();
            TimeEvents.Clear();
            foreach (var e in events)
            {
                TimeEvents.Add(e);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void UpdatePeerInfosFrom(Node node)
        {
            PeerInfos.Clear();
            foreach (var item in node.PeerInfos)
            {
                PeerInfos.Add(item);
            }

            Events.Clear();
            foreach (var item in node.Events)
            {
                Events.Add(item);
            }
        }
    }
}
