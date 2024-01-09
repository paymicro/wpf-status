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

        public TimeEvent() {
            timer = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                (s, e) =>
                {
                    if (EventType == TimeEventTypeEnum.Here)
                    {
                        DateTime = DateTime.Now;
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

        public bool IsSelected {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(InDays));
            }
        }

        DateTime _dateTime;
        public DateTime DateTime
        {
            get => _dateTime;
            set
            {
                _dateTime = value;
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

                var time = _dateTime - DateTime.Now;
                if (time.TotalDays < 0)
                {
                    if (IsSelected)
                    {
                        return _dateTime.ToString("g");
                    }
                    return $"🏁 {Helper.TimeToDaysString(time.Duration())}";
                }

                if (IsSelected)
                {
                    return _dateTime.ToString("g");
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

        public string Desc { get; set; } = string.Empty;

        string _rewardStr = string.Empty;

        public string RewardStr
        {
            get => _rewardStr;
            set
            {
                _rewardStr = value;
                OnPropertyChanged(nameof(RewardStr));
            }
        }

        public Visibility RewardVisible { get; set; } = Visibility.Collapsed;

        public TimeEventTypeEnum EventType { get; set; } = TimeEventTypeEnum.None;

        public int CompareTo(TimeEvent? other) => DateTime.CompareTo(other?.DateTime);

        public void UpdateVarProps(TimeEvent other)
        {
            _rewardStr = other.RewardStr;
            RewardVisible = other.RewardVisible;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
