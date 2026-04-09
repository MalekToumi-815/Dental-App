using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System;
using System.Globalization;
using System.Windows;

namespace Dental_App.Views
{
    public partial class RadioImagesView : UserControl
    {
        public RadioImagesView()
        {
            InitializeComponent();
        }
    }

    public class BooleanToVisibilityConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

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
    }
}
