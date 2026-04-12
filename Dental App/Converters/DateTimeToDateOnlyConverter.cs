using System;
using System.Globalization;
using System.Windows.Data;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertit entre DateTime (du DatePicker WPF) et DateOnly (du ViewModel)
    /// </summary>
    public class DateTimeToDateOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            if (value is DateOnly dateOnly)
            {
                return dateOnly.ToDateTime(TimeOnly.MinValue);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(dateTime);
            }

            return null;
        }
    }
}
