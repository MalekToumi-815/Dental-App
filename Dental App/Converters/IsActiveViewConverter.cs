using System;
using System.Globalization;
using System.Windows.Data;

namespace Dental_App.Converters
{
    public class IsActiveViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string activeView = value.ToString();
            string buttonView = parameter.ToString();

            return activeView.Equals(buttonView, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
