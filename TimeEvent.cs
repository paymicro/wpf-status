namespace WpfStatus
{
    public class TimeEvent: IComparable<TimeEvent>
    {
        DateTime _dateTime;
        public DateTime DateTime
        {
            get => _dateTime;
            set
            {
                _dateTime = value;
                _layer = Helper.GetLayerByTime(value);
            }
        }

        public string InDays
        {
            get
            {
                var time = _dateTime - DateTime.Now;
                if (time.TotalDays < 0)
                {
                    if (time.TotalMinutes > -4)
                    {
                        return DateTime.Now.ToString("T");
                    }
                    else
                    {
                        return $"🏁 {Helper.TimeToDaysString(time.Duration())} ago";
                    }
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
                _dateTime = Helper.GetTimeByLayer(value);
            }
        }

        public string Desc { get; set; } = string.Empty;

        public int EventType { get; set; } = 0;

        public int CompareTo(TimeEvent? other) => DateTime.CompareTo(other?.DateTime);
    }
}
