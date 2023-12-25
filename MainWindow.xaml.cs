﻿using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfStatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        Timer timer;
        readonly AppSettings appSettings;
        readonly SynchronizationContext? uiContext;
        int timerProgress = 0;
        readonly MainViewModel model;

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            appSettings = AppSettings.LoadSettings();
            model = new MainViewModel(appSettings);
            uiContext = SynchronizationContext.Current;
            InitializeComponent();

            DataContext = model;
            UpdateInfo();
        }

        private void UpdateInfo()
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

            var events = new List<TimeEvent>
            {
                new() { DateTime = eCurrentBegin, Desc = $"Epoch {eCurrentNum - 1} End" },
                new() { DateTime = eCurrentBegin.Add(official12hOffset), Desc = $"PoST {eCurrentNum - 1} Begin"},
                new() { DateTime = eCurrentBegin.Add(official12hOffset2), Desc = $"PoST {eCurrentNum - 1} 12h End" },
                new() { DateTime = eCurrentBegin.Add(eDurationMs), Desc = $"PoST {eCurrentNum} 108h End" },
                new() { DateTime = DateTime.Now, Desc = "We are here", EventType = 1 },
            };

            foreach (var node in model.Nodes)
            {
                var eli = node.Events.Select(e => e.Eligibilities).Where(e => e != null).FirstOrDefault();
                if (eli != null)
                {
                    events.AddRange(eli.Eligibilities.Select(r => new TimeEvent() { Layer = r.Layer, Desc = node.Name, EventType = 2 }).ToList());
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

        private void StartTimer(int period = 25_000)
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

        private async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            await model.UpdateAllNodes();
            UpdateInfo();
        }

        private void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        private void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            model.ProgressValue = 0;
            timerProgress = 0;
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
    }
}