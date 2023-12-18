using WpfStatus.api;
using System.ComponentModel;
using System.Text.Json;
using static WpfStatus.Helper;

namespace WpfStatus
{
    public class Node : NodeSetting, INotifyPropertyChanged
    {
        private readonly Helper helper;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Node()
        {
            helper = new Helper();
        }

        public int Connected { get; set; }

        public Status Status { get; private set; } = new();

        public string IsOk
        {
            get
            {
                if (Status.TopLayer.Number == 0)
                {
                    return "Offline";
                }
                else
                {
                    return (Status.IsSynced == true && Status.TopLayer.Number == Status.SyncedLayer.Number).ToString();
                }
            }
        }

        public PostSetupStatus PostSetupStatus { get; set; } = new();

        public bool IsUpdatePostSetup { get; set; } = true;

        public List<Event> Events { get; set; } = [];

        public bool IsUpdateEvents { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.MinValue;

        public string LastUpdatedStr => LastUpdated.ToString("T");

        public string Rewards
        {
            get
            {
                var eli = Events.Select(e => e.Eligibilities).Where(e => e != null).FirstOrDefault();
                if (eli != null)
                {
                    return string.Join(" ", eli.Eligibilities.Select(e => e.Layer));
                }
                return string.Empty;
            }
        }

        public string TimeToNextReward
        {
            get
            {
                var rewards = Rewards.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(r => int.Parse(r));
                var top = Status?.TopLayer.Number ?? 0;
                if (!rewards.Any() || top == 0)
                {
                    return string.Empty;
                }

                var reward = rewards.FirstOrDefault(r => r > top);
                if (reward != 0)
                {
                    var min = (reward - top) * 5;;
                    return TimeSpan.FromMinutes(min).ToString(@"d\d\ hh\:mm");
                }

                return "----";
            }
        }

        public async Task Update()
        {
            Status = await GetStatus();
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsOk));
            LastUpdated = DateTime.Now;
            OnPropertyChanged(nameof(LastUpdatedStr));

            if (string.IsNullOrEmpty(Status.ConnectedPeers))
            {
                return;
            }

            if (IsUpdatePostSetup)
            {
                PostSetupStatus = await GetPostSetupStatus();
                OnPropertyChanged(nameof(PostSetupStatus));
            }

            if (IsUpdateEvents)
            {
                Events = await GetEventsStream();
                OnPropertyChanged(nameof(Events));
                OnPropertyChanged(nameof(Rewards));
                OnPropertyChanged(nameof(TimeToNextReward));
                IsUpdateEvents = false;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<Status> GetStatus()
        {
            var output = await helper.CallGPRC(Host, Port, "spacemesh.v1.NodeService.Status", maxTime: 3);

            return string.IsNullOrEmpty(output)
                ? new()
                : Json.Deserialize(output, new { Status = new Status() })?.Status ?? new();
        }

        private async Task<PostSetupStatus> GetPostSetupStatus()
        {
            var output = await helper.CallGPRC(Host, AdminPort, "spacemesh.v1.SmesherService.PostSetupStatus", maxTime: 3);

            return string.IsNullOrEmpty(output)
                ? new()
                : Json.Deserialize(output, new { Status = new PostSetupStatus() })?.Status ?? new();
        }

        private async Task<List<Event>> GetEventsStream()
        {
            var output = await helper.CallGPRC(Host, AdminPort, "spacemesh.v1.AdminService.EventsStream", maxTime: 1);

            output = "[" + output.Replace("\r\n}\r\n{", "\r\n},\r\n{") + "]"; // fix Json array
            return string.IsNullOrEmpty(output)
                ? []
                : JsonSerializer.Deserialize<List<Event>>(output, Json.SerializerOptions) ?? [];
        }
    }
}
