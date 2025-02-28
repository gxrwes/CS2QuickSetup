using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ConfigCreator.Converters
{
    public class BoolToRecordingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.Red : Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
