using System;
using System.Globalization;
using System.Windows.Data;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertit une DateTime en chaîne formatée "MMMM yyyy" (ex: "Mars 2026")
    /// </summary>
    public class DateToMonthYearConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // Utiliser la culture française pour les noms de mois
                var frenchCulture = new CultureInfo("fr-FR");
                return dateTime.ToString("MMMM yyyy", frenchCulture);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
