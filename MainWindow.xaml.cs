using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using WpfStatus.Notification;

namespace WpfStatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        DispatcherTimer? timer;
        readonly AppSettings appSettings;
        int timerProgress = 0;
        readonly MainViewModel model;

        DateTime lastNotification = DateTime.MinValue;
        readonly Telegram? telegram;

        GridViewColumnHeader? _lastHeaderClicked;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            appSettings = AppSettings.LoadSettings();
            model = new MainViewModel(appSettings);
            InitializeComponent();

            DataContext = model;
            if (appSettings.NotificationSettings.TelegramApiId != 0 && !string.IsNullOrEmpty(appSettings.NotificationSettings.TelegramApiHash))
            {
                telegram = new Telegram(appSettings.NotificationSettings);
            }
        }

        void StartTimer(int period = 30_000)
        {
            StopTimer();
            timer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(50),
                DispatcherPriority.DataBind,
                (s, e) =>
                {
                    timerProgress += 50;
                    model.ProgressValue = 100f * timerProgress / period;
                    if (timerProgress > period)
                    {
                        timerProgress = 0;
                        UpdateAll_Click(this, new RoutedEventArgs());
                    }
                },
                Dispatcher.CurrentDispatcher);
        }

        void StopTimer()
        {
            model.ProgressValue = 0;
            timerProgress = 0;
            timer?.Stop();
        }

        async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Node node)
            {
                node.IsUpdateEvents = true;
                node.IsUpdatePostSetup = true;
                await node.Update();
                model.UpdatePeerInfo();
            }
            else
            {
                MessageBox.Show("Unable to update");
            }
        }

        async Task CheckNotifications()
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

        async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            await model.UpdateAllNodes();
            await CheckNotifications();
        }

        void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        protected override void OnClosed(EventArgs e)
        {
            // AppSettings.SaveSettings(appSettings);
            telegram?.Dispose();
            base.OnClosed(e);
        }

        void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0 && e.AddedItems[0] is Node node) {
                model.SelectedNode = node;

                var newIndex = false;
                var eventInFuture = model.TimeEvents.FirstOrDefault(t => t.DateTime > DateTime.Now && t.Desc == node.Name);
                if (eventInFuture != null)
                {
                    ListTimeEvents.SelectedItem = eventInFuture;
                    newIndex = true;
                }
                else
                {
                    var eventInPast = model.TimeEvents.FirstOrDefault(t => t.DateTime < DateTime.Now && t.Desc == node.Name);
                    if (eventInPast != null)
                    {
                        ListTimeEvents.SelectedItem = eventInPast;
                        newIndex = true;
                    }
                }
                if (newIndex)
                {
                    ListTimeEvents.ScrollIntoView(ListTimeEvents.SelectedItem);
                }
            }
        }

        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
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

        void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(List.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        void ListTimeEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0 && e.AddedItems[0] is TimeEvent timeEvent)
            {
                for (var i = 0; i < List.Items.Count; i++)
                {
                    if (List.Items[i] is Node node && node.Name == timeEvent.Desc)
                    {
                        List.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        void Action_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Control button &&
                button.Tag is CustomAction customAction &&
                !string.IsNullOrEmpty(customAction.Script))
            {
                Helper.RunPowerShell(customAction.Script);
            }
        }
    }
}