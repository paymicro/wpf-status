using MahApps.Metro.Controls;
using WpfStatus.api;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace WpfStatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        Timer timer;
        AppSettings appSettings;
        int timerProgress = 0;
        MainViewModel model;

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            appSettings = AppSettings.LoadSettings();
            model = new MainViewModel(appSettings);
            InitializeComponent();

            DataContext = model;
        }

        private void StartTimer(int period = 15_000)
        {
            timer = new Timer(_ =>
            {
                timerProgress += 500;
                model.ProgressValue = 100 * timerProgress / period;
                if (timerProgress > period)
                {
                    UpdateAll_Click(this, new RoutedEventArgs());
                    timerProgress = 0;
                }
            }, null, 0, 500);
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Node node)
            {
                button.IsEnabled = false;
                node.IsUpdateEvents = true;
                await node.Update();
                button.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Unable to update");
            }
        }

        private async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in model.Nodes)
            {
                await node.Update();
            }
        }

        private void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        private void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            model.ProgressValue = 0;
            timer.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            // AppSettings.SaveSettings(appSettings);
            timer.Dispose();
            base.OnClosed(e);
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is Node node) {
                EventsText.Text = string.Join(Environment.NewLine, node.Events.Select(EventToString));

                model.PeerInfos.Clear();
                foreach (var item in node.PeerInfos)
                {
                    model.PeerInfos.Add(item);
                }
            }
        }

        private string EventToString(Event e)
        {
            var date = DateTime.Parse(e.Timestamp).ToString("G");
            var result = date + "\t" + e.Help;
            if (e.Eligibilities != null)
            {
                var eli = string.Join("  ", e.Eligibilities.Eligibilities.Select(el => el.Layer.ToString()));
                result += Environment.NewLine + "Eligibilities: " + eli + Environment.NewLine;
            }
            return result;
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
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
    }
}