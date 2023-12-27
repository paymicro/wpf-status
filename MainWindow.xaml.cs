using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfStatus.Notification;

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

        GridViewColumnHeader? _lastHeaderClicked;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            appSettings = AppSettings.LoadSettings();
            model = new MainViewModel(appSettings);
            uiContext = SynchronizationContext.Current;
            InitializeComponent();

            DataContext = model;
            if (appSettings.NotificationSettings.TelegramApiId != 0 && !string.IsNullOrEmpty(appSettings.NotificationSettings.TelegramApiHash))
            {
                telegram = new Telegram(appSettings.NotificationSettings);
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
            timer?.Dispose();
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
            // TODO
            var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
        }
    }
}