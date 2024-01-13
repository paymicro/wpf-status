using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfStatus
{
    public class MarginLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return new Thickness(5 + 5 * level, 2, 5, 2);
            }
            return new Thickness(5, 2, 5, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
