using System;
using System.Globalization;
using System.Windows.Data;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne à son inverse (true -> false, false -> true).
    /// Utile pour les bindings d'IsEnabled et similaires.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return value;
        }
    }
}
