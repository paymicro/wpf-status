using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfStatus.Notification;
using static WpfStatus.Helper;

namespace WpfStatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        Timer? timer;
        readonly AppSettings appSettings;
        readonly SynchronizationContext? uiContext;
        int timerProgress = 0;
        readonly MainViewModel model;

        DateTime lastNotification = DateTime.MinValue;
        readonly Telegram? telegram;

        GridViewColumnHeader _lastHeaderClicked;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            appSettings = AppSettings.LoadSettings();
            model = new MainViewModel(appSettings);
            uiContext = SynchronizationContext.Current;
            InitializeComponent();

            DataContext = model;
            UpdateInfo();
            if (appSettings.NotificationSettings.TelegramApiId != 0 && !string.IsNullOrEmpty(appSettings.NotificationSettings.TelegramApiHash))
            {
                telegram = new Telegram(appSettings.NotificationSettings);
            }
        }

        private async void UpdateInfo()
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

            var rewards = new List<RewardEntity>();

            if (!string.IsNullOrWhiteSpace(appSettings.Coinbase))
            {
                try
                {
                    using var client = new HttpClient();

                    var result = await client.GetStringAsync($"https://mainnet-explorer-api.spacemesh.network/accounts/{appSettings.Coinbase}/rewards?page=1&pagesize=500");
                    rewards = Json.Deserialize(result, new { Data = new List<RewardEntity>(), Paginatiaon = new object() })?.Data ?? [];

                    rewards = rewards.Where(r => r.Layer > beginEpohLayer).ToList();
                }
                catch
                {
                    rewards = [];
                }
            }

            foreach (var node in model.Nodes)
            {
                var eli = node.Events.Select(e => e.Eligibilities).Where(e => e != null).FirstOrDefault();
                if (eli != null)
                {
                    var preparedEvents = eli.Eligibilities.Select(r => new TimeEvent() { Layer = r.Layer, Desc = node.Name, EventType = 2 }).ToList();
                    foreach (var e in preparedEvents.Where(prep => prep.Layer <= currentLayer))
                    {
                        var reward = rewards.FirstOrDefault(r => r.Layer == e.Layer);
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

            model.Info = string.Join(Environment.NewLine, eventsStr);
            events.Sort();
            model.TimeEvents.Clear();
            foreach (var e in events)
            {
                model.TimeEvents.Add(e);
            }
        }

        private void StartTimer(int period = 30_000)
        {
            timer = new Timer(_ =>
            {
                timerProgress += 500;
                model.ProgressValue = 100 * timerProgress / period;
                if (timerProgress > period)
                {
                    timerProgress = 0;
                    uiContext?.Send(x =>
                    {
                        UpdateAll_Click(this, new RoutedEventArgs());
                    }, null);
                }
            }, null, 0, 500);
        }

        private void StopTimer()
        {
            model.ProgressValue = 0;
            timerProgress = 0;
            timer.Dispose();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Node node)
            {
                button.IsEnabled = false;
                node.IsUpdateEvents = true;
                await node.Update();
                model.SelectedNode = node;
                button.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Unable to update");
            }
        }

        private async Task CheckNotifications()
        {
            if (!model.IsEnabledNotifications || (DateTime.Now - lastNotification).TotalSeconds < appSettings.NotificationSettings.DalaySec)
            {
                return;
            }

            if (telegram != null)
            {
                var invalidNodes = model.Nodes.Where(n => n.IsOk != "✔");
                if (invalidNodes.Any())
                {
                    var message = string.Join(Environment.NewLine, invalidNodes.Select(n => $"{n.Name} is {n.IsOk} | {n.Status}"));
                    await telegram.Send(message);
                    lastNotification = DateTime.Now;
                }
            }
        }

        private async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            await model.UpdateAllNodes();
            UpdateInfo();
            await CheckNotifications();
        }

        private void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        private void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        protected override void OnClosed(EventArgs e)
        {
            // AppSettings.SaveSettings(appSettings);
            timer?.Dispose();
            telegram?.Dispose();
            base.OnClosed(e);
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is Node node) {
                model.SelectedNode = node;
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender,
                                            RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(List.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
        }
    }
}