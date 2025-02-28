using System;
using System.Globalization;
using System.Windows.Data;

namespace ConfigCreator.Converters
{
    public class BoolToRecordingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "RECORDING" : "Record";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
