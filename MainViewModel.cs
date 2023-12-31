﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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

        float _progressValue;

        public float ProgressValue
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
                    UpdatePeerInfo();
                }
                OnPropertyChanged(nameof(SelectedNode));
            }
        }

        DateTime lastUpdateAll = DateTime.MinValue;

        public async Task UpdateAllNodes()
        {
            foreach (var node in Nodes)
            {
                if ((DateTime.UtcNow - lastUpdateAll).TotalMinutes > 5)
                {
                    lastUpdateAll = DateTime.UtcNow;
                    node.IsUpdateEvents = true;
                    node.IsUpdatePostSetup = true;
                }

                await node.Update();
            }

            UpdatePeerInfo();
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
                TimeEvents.Add(new() { DateTime = eCurrentBegin, Desc = $"Epoch {eCurrentNum}" });
                TimeEvents.Add(new() { DateTime = eCurrentBegin.Add(official12hOffset), Desc = $"⚡ PoST Begin ⚡" });
                TimeEvents.Add(new() { DateTime = eCurrentBegin.Add(official12hOffset2), Desc = $"🚧 PoST End 🚧" });
                TimeEvents.Add(new() { DateTime = eCurrentBegin.Add(eDurationMs), Desc = $"PoST {eCurrentNum} 108h End" });
                TimeEvents.Add(new() { DateTime = now, Desc = "We are here", EventType = Enums.TimeEventTypeEnum.Here });
            }

            if (updateRewards &&
                !string.IsNullOrWhiteSpace(appSettings.Coinbase) &&
                appSettings.Coinbase.StartsWith("sm1qqqqqq") &&
                (now - lastGettingRewards).TotalMinutes > 2)
            {
                using var client = new HttpClient();
                var result = await client.GetStringAsync($"https://mainnet-explorer-api.spacemesh.network/accounts/{appSettings.Coinbase}/rewards?page=1&pagesize=200");
                var rewards = Json.Deserialize(result, new { Data = new List<RewardEntity>(), Paginatiaon = new object() })?.Data ?? [];
                lastGettingRewards = now;
                RewardsList = rewards.Where(r => r.Layer > beginEpohLayer).ToList();
                addRewardResults = true;
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
                        EventType = Enums.TimeEventTypeEnum.Reward
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

                var rewardTypes = new List<Enums.TimeEventTypeEnum>()
                {
                    Enums.TimeEventTypeEnum.Reward,
                    Enums.TimeEventTypeEnum.CloseReward
                };

                foreach (var item in preparedEvents)
                {
                    var days = (now - item.DateTime).TotalDays;
                    if (days > 0 && days < 0.5)
                    {
                        item.EventType = Enums.TimeEventTypeEnum.CloseReward;
                    }

                    var contains = TimeEvents.FirstOrDefault(e => e.Layer == item.Layer
                        && rewardTypes.Contains(e.EventType)
                        && e.Desc == item.Desc);
                    if (contains != default)
                    {
                        contains.UpdateVarProps(item);
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
