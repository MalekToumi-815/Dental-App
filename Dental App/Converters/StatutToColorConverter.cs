using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertit le statut d'un rendez-vous en couleur
    /// "en attente" = Jaune/Orange
    /// "termine" = Vert
    /// "annule" = Rouge
    /// </summary>
    public class StatutToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statut)
            {
                return statut.ToLower() switch
                {
                    "en attente" => new SolidColorBrush(Color.FromArgb(255, 255, 193, 7)), // Ambre/Jaune
                    "termine" => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),   // Vert
                    "annule" => new SolidColorBrush(Color.FromArgb(255, 244, 67, 54)),    // Rouge
                    _ => new SolidColorBrush(Color.FromArgb(255, 158, 158, 158))          // Gris par défaut
                };
            }

            return new SolidColorBrush(Color.FromArgb(255, 158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
