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
        private readonly AppSettings appSettings;

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
            UpdateInfo();
        }

        private int _progressValue;

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

        public ObservableCollection<Node> Nodes { get; set; } = [];

        public ObservableCollection<PeerInfo> PeerInfos { get; set; } = [];

        public ObservableCollection<Event> Events { get; set; } = [];

        private string _info = $"--- Info ---{Environment.NewLine}Loading...";

        public string Info
        {
            get { return _info; }
            set
            {
                _info = value;
                OnPropertyChanged(nameof(Info));
            }
        }

        public ObservableCollection<TimeEvent> TimeEvents { get; set; } = [ new() { DateTime = DateTime.Now, Desc = "loading..." } ];

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

        private int updateAllCounter = 0;

        public async Task UpdateAllNodes()
        {
            updateAllCounter++;
            if (updateAllCounter > 5)
            {
                updateAllCounter = 0;
            }

            foreach (var node in Nodes)
            {
                if (updateAllCounter == 0)
                {
                    node.IsUpdateEvents = true;
                    node.IsUpdatePostSetup = true;
                }

                await node.Update();
            }

            if (_selectedNode != null)
            {
                UpdatePeerInfosFrom(_selectedNode);
            }
        }

        private List<RewardEntity> RewardsList = [];

        private DateTime lastGettingRewards = DateTime.MinValue;

        public async Task UpdateInfo()
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

            if (!string.IsNullOrWhiteSpace(appSettings.Coinbase) && (DateTime.Now - lastGettingRewards).TotalMinutes > 2)
            {
                using var client = new HttpClient();
                var result = await client.GetStringAsync($"https://mainnet-explorer-api.spacemesh.network/accounts/{appSettings.Coinbase}/rewards?page=1&pagesize=500");
                var rewards = Json.Deserialize(result, new { Data = new List<RewardEntity>(), Paginatiaon = new object() })?.Data ?? [];
                lastGettingRewards = DateTime.Now;
                RewardsList = rewards.Where(r => r.Layer > beginEpohLayer).ToList();
            }

            foreach (var node in Nodes)
            {
                var eli = node.Events.Select(e => e.Eligibilities).Where(e => e != null).FirstOrDefault();
                if (eli != null)
                {
                    var preparedEvents = eli.Eligibilities.Select(r => new TimeEvent() { Layer = r.Layer, Desc = node.Name, EventType = 2 }).ToList();
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
            }

            var eventsStr = events.Select(e =>
                (e.DateTime - DateTime.Now).TotalHours.ToString("0.0") + "h" +
                Environment.NewLine + e.Desc);

            Info = string.Join(Environment.NewLine, eventsStr);
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

        private void UpdatePeerInfosFrom(Node node)
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
