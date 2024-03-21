using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using WpfStatus.api;
using WpfStatus.Notification;
using static WpfStatus.Helper;

namespace WpfStatus
{
    public class MainViewModel : INotifyPropertyChanged
    {
        readonly AppSettings appSettings;
        readonly List<ExportNodeLayer> nodeLayers;

        DateTime lastNotification = DateTime.MinValue;
        readonly Telegram? telegram;

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
                    PostPort = node.PostPort,
                });
            }
            MainWindowTitle = appSettings.AppTitle;
            IsAutoUpdate = appSettings.IsTimerEnabled;
            IsEnabledNotifications = appSettings.NotificationSettings.Enabled;
            nodeLayers = ExportNodeLayer.Load();
            if (appSettings.NotificationSettings.TelegramApiId != 0 && !string.IsNullOrEmpty(appSettings.NotificationSettings.TelegramApiHash))
            {
                telegram = new Telegram(appSettings.NotificationSettings);
            }
            _ = UpdateInfo();
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
                    UpdatePeerInfo();
                }
                OnPropertyChanged(nameof(SelectedNode));
            }
        }

        DateTime lastDeepUpdate = DateTime.MinValue;

        public async Task UpdateAllNodes()
        {
            var deepUpdate = false;
            if ((DateTime.UtcNow - lastDeepUpdate).TotalMinutes > 10)
            {
                lastDeepUpdate = DateTime.UtcNow;
                deepUpdate = true;
            }

            foreach (var node in Nodes)
            {
                if (deepUpdate)
                {
                    node.IsUpdateEvents = true;
                    node.IsUpdatePostSetup = true;
                }

                await node.Update();
            }

            UpdatePeerInfo();
            await UpdateInfo();
            ExportNodeLayer.Save(nodeLayers);

            if (IsEnabledNotifications)
            {
                var nearRewards = TimeEvents.Where(t => t.GetDays > 0 && t.GetDays < 0.5 && t.IsReward);
                var rewardsNames = nearRewards.Select(t => t.Name);
                var alarmNodes = Nodes.Where(n => !n.Status.IsSynced && rewardsNames.Contains(n.Name));
                if (alarmNodes.Any())
                {
                    var message = string.Join(Environment.NewLine, alarmNodes
                        .Select(n => $"{n.Name}{Environment.NewLine}" +
                        $"   Next reward {nearRewards.FirstOrDefault(r => r.Name == n.Name)?.InDays}.{Environment.NewLine}" +
                        $"   {n.IsOk} | {n.Status}"));
                    await SendNotification(message);
                }
            }
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

        public async Task SendNotification(string message)
        {
            if (!IsEnabledNotifications || (DateTime.UtcNow - lastNotification).TotalSeconds < appSettings.NotificationSettings.DalaySec)
            {
                return;
            }

            if (telegram != null)
            {
                await telegram.Send(message);
                lastNotification = DateTime.UtcNow;
            }
        }

        string GetCoinBase()
        {
            appSettings.Coinbase = (!string.IsNullOrWhiteSpace(appSettings.Coinbase) && appSettings.Coinbase.StartsWith("sm1qqqqqq"))
                ? appSettings.Coinbase
                : Nodes.FirstOrDefault(n => n.Coinbase.StartsWith("sm1qqqqqq"))?.Coinbase ?? string.Empty;
            return appSettings.Coinbase;
        }

        async Task UpdateInfo()
        {
            var now = DateTime.UtcNow;

            var markEpochNumber = 6;
            var markEpochBegin = DateTime.Parse("2023-10-06 8:00:00Z", DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AdjustToUniversal);
            var eDurationMs = TimeSpan.FromDays(14);  // 2 weeks
            var official12hOffset = TimeSpan.FromDays(9.5); // -228h
            var official12hOffset2 = TimeSpan.FromDays(10); // +12h
            var ePassedNum = (int)((now - markEpochBegin) / eDurationMs);
            var eCurrentNum = markEpochNumber + ePassedNum;
            var eCurrentBegin = markEpochBegin.Add(eDurationMs * (eCurrentNum - markEpochNumber));
            var beginEpohLayer = GetLayerByTime(eCurrentBegin);
            var currentLayer = GetLayerByTime(now);
            var addRewardResults = false;

            if (TimeEvents.Count == 0)
            {
                TimeEvents.Add(new() { DateTime = eCurrentBegin, Name = $"Epoch {eCurrentNum}", Level = -1 });
                TimeEvents.Add(new() { DateTime = eCurrentBegin.Add(official12hOffset), Name = $"PoST Begin", Level = 1 });
                TimeEvents.Add(new() { DateTime = eCurrentBegin.Add(official12hOffset2), Name = $"PoST End", Level = 1 });
                TimeEvents.Add(new() { DateTime = eCurrentBegin.Add(eDurationMs), Name = $"PoST {eCurrentNum} 108h End", Level = -1 });
                TimeEvents.Add(new() { DateTime = now, Name = "We are here", EventType = Enums.TimeEventTypeEnum.Here });
            }

            if (!string.IsNullOrWhiteSpace(GetCoinBase()) &&
                (now - lastGettingRewards).TotalSeconds > 10)
            {
                try
                {
                    using var client = new HttpClient();
                    var result = await client.GetStringAsync($"https://mainnet-explorer-api.spacemesh.network/accounts/{appSettings.Coinbase}/rewards?page=1&pagesize=200");
                    var rewards = Json.Deserialize(result, new { Data = new List<RewardEntity>(), Paginatiaon = new object() })?.Data ?? [];
                    var lastRewardList = rewards.Where(r => r.Layer > beginEpohLayer).ToList();
                    var diff = lastRewardList.Count - RewardsList.Count;
                    if (diff > 0 && lastGettingRewards > DateTime.MinValue)
                    {
                        var message = $"Reward:";
                        for (var i = 0; i < diff; i++)
                        {
                            message += $"{Environment.NewLine} +{Math.Round(lastRewardList[i].Total / 1000_000_000d, 3)}";
                        }
                        await SendNotification(message);
                    }
                    RewardsList = lastRewardList;
                    addRewardResults = true;
                    lastGettingRewards = now;
                } catch (Exception) { }
            }

            foreach (var node in Nodes)
            {
                var rewards = node.Rewards;
                List<TimeEvent> preparedEvents = [];
                if (!string.IsNullOrEmpty(rewards))
                {
                    var layers = rewards.Split(' ');
                    UpdateSavedLayers(node.Name, string.Join(',', layers));
                    preparedEvents = layers.Select(l => new TimeEvent()
                    {
                        Layer = Convert.ToInt32(l),
                        Name = node.Name,
                        EventType = Enums.TimeEventTypeEnum.Reward
                    }).ToList();
                }
                else
                {
                    var saved = nodeLayers.FirstOrDefault(n => n.NodeName == node.Name);
                    if (saved != default && saved.Eligibilities.Length != 0)
                    {
                        preparedEvents = saved.Eligibilities.Split(',').Select(l => new TimeEvent()
                        {
                            Layer = Convert.ToInt32(l),
                            Name = node.Name,
                            EventType = Enums.TimeEventTypeEnum.Reward
                        }).ToList();
                    }
                }
                if (addRewardResults)
                {
                    foreach (var e in preparedEvents.Where(prep => prep.Layer <= currentLayer))
                    {
                        var reward = RewardsList.FirstOrDefault(r => r.Layer == e.Layer);
                        if (reward != null)
                        {
                            e.RewardStr = $"+{Math.Round(reward.Total / 1000_000_000d, 3)}";
                        }
                        else
                        {
                            e.RewardStr = "❌ Missed";
                        }
                        e.RewardVisible = Visibility.Visible;
                    }
                }

                foreach (var item in preparedEvents)
                {
                    var hours = (now - item.DateTime).TotalHours;
                    if (hours > 0 && hours < 12)
                    {
                        item.EventType = Enums.TimeEventTypeEnum.CloseReward;
                    }

                    var contains = TimeEvents.FirstOrDefault(e => e.Layer == item.Layer
                        && e.IsReward
                        && e.Name == item.Name);
                    if (contains != default)
                    {
                        if (addRewardResults)
                        {
                            contains.UpdateVarProps(item);
                        }
                    }
                    else
                    {
                        TimeEvents.Add(item);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdatePeerInfo()
        {
            if (_selectedNode == null)
            {
                return;
            }

            PeerInfos.Clear();
            foreach (var item in _selectedNode.PeerInfos)
            {
                PeerInfos.Add(item);
            }

            Events.Clear();
            foreach (var item in _selectedNode.Events)
            {
                Events.Add(item);
            }
        }
    }
}
