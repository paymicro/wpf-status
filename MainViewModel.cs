using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using WpfStatus.api;

namespace WpfStatus
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel(AppSettings appSettings)
        {
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
