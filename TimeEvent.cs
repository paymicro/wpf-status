﻿using System.ComponentModel;
using System.Windows;

namespace WpfStatus
{
    public class TimeEvent : IComparable<TimeEvent>, INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        readonly Timer timer;

        public TimeEvent() {
            timer = new (_ =>
            {
                if (EventType == 1)
                {
                    DateTime = DateTime.Now;
                    OnPropertyChanged(nameof(Layer));
                }
                else
                {
                    OnPropertyChanged(nameof(InDays));
                }
            }, null, 0, 5000);
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
                if (EventType == 1)
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
                    return $"🏁 {Helper.TimeToDaysString(time.Duration())} ago";
                }

                if (IsSelected)
                {
                    return _dateTime.ToString("g");
                }

                return $"in {Helper.TimeToDaysString(time)}";
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

        public int EventType { get; set; } = 0;

        public int CompareTo(TimeEvent? other) => DateTime.CompareTo(other?.DateTime);

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}
