using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using WpfStatus.Enums;

namespace WpfStatus
{
    public class TimeEvent : IComparable<TimeEvent>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        readonly DispatcherTimer timer;

        public TimeEvent()
        {
            timer = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                (s, e) =>
                {
                    if (EventType == TimeEventTypeEnum.Here)
                    {
                        DateTime = DateTime.UtcNow;
                        OnPropertyChanged(nameof(Layer));
                    }
                    else
                    {
                        OnPropertyChanged(nameof(InDays));
                    }
                },
                Dispatcher.CurrentDispatcher);
        }

        bool _isSelected = false;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(InDays));
            }
        }

        public int Level { get; set; } = 0;

        DateTime _dateTime;

        /// <summary>
        /// DateTime in UTC
        /// </summary>
        public DateTime DateTime
        {
            get => _dateTime;
            set
            {
                _dateTime = value.ToUniversalTime();
                _layer = Helper.GetLayerByTime(value);
                OnPropertyChanged(nameof(DateTime));
                OnPropertyChanged(nameof(InDays));
            }
        }

        public string InDays
        {
            get
            {
                if (EventType == TimeEventTypeEnum.Here)
                {
                    return DateTime.Now.ToString("T");
                }

                var time = _dateTime - DateTime.UtcNow;
                if (time.TotalDays < 0)
                {
                    if (IsSelected)
                    {
                        return _dateTime.Add(TimeZoneInfo.Local.BaseUtcOffset).ToString("g");
                    }
                    return $"- {Helper.TimeToDaysString(time.Duration())}";
                }

                if (IsSelected)
                {
                    return _dateTime.Add(TimeZoneInfo.Local.BaseUtcOffset).ToString("g");
                }

                return $"+ {Helper.TimeToDaysString(time)}";
            }
        }

        int _layer = 0;

        public int Layer
        {
            get => _layer;
            set
            {
                _layer = value;
                DateTime = Helper.GetTimeByLayer(value);
            }
        }

        /// <summary>
        /// The name of the node. If it is not a node, then the name of the event.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        string _rewardStr = string.Empty;

        public string RewardStr
        {
            get => _rewardStr;
            set
            {
                var isChanged = _rewardStr != value;
                _rewardStr = value;
                if (isChanged)
                {
                    OnPropertyChanged(nameof(RewardStr));
                }
            }
        }

        Visibility _rewardVisible = Visibility.Collapsed;

        public Visibility RewardVisible
        {
            get => _rewardVisible;
            set
            {
                var isChanged = _rewardVisible != value;
                _rewardVisible = value;
                if (isChanged)
                {
                    OnPropertyChanged(nameof(RewardVisible));
                }
            }
        }

        public TimeEventTypeEnum EventType { get; set; } = TimeEventTypeEnum.None;

        public int CompareTo(TimeEvent? other) => DateTime.CompareTo(other?.DateTime);

        public void UpdateVarProps(TimeEvent other)
        {
            RewardStr = other.RewardStr;
            RewardVisible = other.RewardVisible;
        }

        internal double GetDays
        {
            get => (_dateTime - DateTime.UtcNow).TotalDays;
        }

        internal bool IsReward
        {
            get => EventType == TimeEventTypeEnum.Reward || EventType == TimeEventTypeEnum.CloseReward;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
