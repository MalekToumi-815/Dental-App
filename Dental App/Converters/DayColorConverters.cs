using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertisseur pour déterminer la couleur de fond d'un jour du calendrier annuel
    /// </summary>
    public class DayBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)); // CardBackground

            bool isCurrentMonth = (bool)values[0];
            bool isSpecialDay = (bool)values[1];

            if (!isCurrentMonth)
                return new SolidColorBrush(Color.FromArgb(255, 13, 17, 23)); // PrimaryBackground (grayed out)

            if (isSpecialDay)
                return new SolidColorBrush(Color.FromArgb(255, 139, 0, 0)); // Red (Crimson) for holidays and Sundays

            return new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)); // CardBackground
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour déterminer la couleur du texte d'un jour du calendrier annuel
    /// </summary>
    public class DayTextColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // TextPrimary

            bool isCurrentMonth = (bool)values[0];
            bool isSpecialDay = (bool)values[1];

            if (!isCurrentMonth)
                return new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)); // TextSecondary (grayed out)

            if (isSpecialDay)
                return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // White text for special days

            return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // TextPrimary
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour extraire l'année d'un DateTime
    /// </summary>
    public class YearConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return dateTime.Year;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour extraire le mois d'un DateTime
    /// </summary>
    public class MonthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return dateTime.Month;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour convertir un DateTime en chaîne de caractères
    /// </summary>
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd");
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string dateString && DateTime.TryParse(dateString, out var result))
                return result;
            return null;
        }
    }

    /// <summary>
    /// Convertisseur pour convertir une chaîne de caractères en DateTime
    /// </summary>
    public class StringToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string dateString && DateTime.TryParse(dateString, out var result))
                return result;
            return DateTime.Now;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd");
            return string.Empty;
        }
    }
}
