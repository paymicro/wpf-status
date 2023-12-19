using System.Collections.ObjectModel;
using System.ComponentModel;
using WpfStatus.api;

namespace WpfStatus
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private int _progressValue;

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

        private Node? _selectedNode;

        public Node? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (value != null)
                {
                    UpdatePeerInfosFrom(value);
                }
                OnPropertyChanged(nameof(SelectedNode));
            }
        }

        public async Task UpdateAllNodes()
        {
            foreach (var node in Nodes)
            {
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
