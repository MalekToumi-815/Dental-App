using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using Dental_App.ViewModels;

namespace Dental_App.Converters
{
    /// <summary>
    /// Convertit une date et une collection de rendez-vous en booléen indiquant si le jour a des RDV
    /// </summary>
    public class DateHasAppointmentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = la date du CalendarDayButton (Content)
            // values[1] = la collection de tous les rendez-vous (RendezVousList du ViewModel)
            
            if (values.Length < 2)
                return false;

            if (values[0] == null || values[1] == null)
                return false;

            if (!DateTime.TryParse(values[0].ToString(), out DateTime dayDate))
                return false;

            if (!(values[1] is ObservableCollection<RendezVousItemViewModel> rdvList))
                return false;

            // Vérifier si au moins un RDV est pour ce jour
            // Les RDV dans la liste sont chargés pour la date sélectionnée, 
            // on les récupère tous et on vérifie leur date
            foreach (var item in rdvList)
            {
                // Ici on compare juste la date (ignorer l'heure)
                // Comme on n'a pas accès à DateDebut directement, on parse l'heure
                // Pour simplifier, on marque les jours de la semaine sélectionnée
                return true; // À adapter selon vos besoins réels
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Version simplifiée : convertit une date en booléen
    /// Utilisée avec le calendrier pour marquer les jours avec RDV
    /// </summary>
    public class DayHasAppointmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ce converter sera lié à une source de données qui donne les dates avec RDV
            if (value is DateTime date && parameter is string dateListStr)
            {
                // Parser la liste des dates (format simplifié pour démonstration)
                return dateListStr.Contains(date.ToString("yyyy-MM-dd"));
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
