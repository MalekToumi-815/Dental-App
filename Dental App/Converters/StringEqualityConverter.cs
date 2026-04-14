using System;
using System.Globalization;
using System.Windows.Data;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertit une chaîne en booléen en vérifiant l'égalité avec un paramètre
    /// Utilisé pour la liaison des ToggleButton avec du texte horaire
    /// </summary>
    public class StringEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                return parameter.ToString();
            }

            return null;
        }
    }
}
