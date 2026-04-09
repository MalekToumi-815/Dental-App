using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dental_App.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = parameter != null && parameter.ToString() == "True";
            
            if (value is bool boolValue)
            {
                bool result = isInverted ? !boolValue : boolValue;
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (value is int intValue)
            {
                bool result = isInverted ? intValue == 0 : intValue != 0;
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0)
            {
                if (values[0] is int count)
                {
                    // For count values, show when count is 0 (no images)
                    return count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                
                if (values[0] is bool boolValue)
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
