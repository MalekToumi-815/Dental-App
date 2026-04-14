using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Dental_App.Behaviors
{
    /// <summary>
    /// Propriété attachée pour afficher des indicateurs sur les jours du calendrier
    /// </summary>
    public static class CalendarBehavior
    {
        public static readonly DependencyProperty DatesWithAppointmentsProperty =
            DependencyProperty.RegisterAttached(
                "DatesWithAppointments",
                typeof(ObservableCollection<DateTime>),
                typeof(CalendarBehavior),
                new PropertyMetadata(null, OnDatesWithAppointmentsChanged));

        public static ObservableCollection<DateTime> GetDatesWithAppointments(DependencyObject obj)
        {
            return (ObservableCollection<DateTime>)obj.GetValue(DatesWithAppointmentsProperty);
        }

        public static void SetDatesWithAppointments(DependencyObject obj, ObservableCollection<DateTime> value)
        {
            obj.SetValue(DatesWithAppointmentsProperty, value);
        }

        private static void OnDatesWithAppointmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Calendar calendar)
            {
                if (e.NewValue is ObservableCollection<DateTime> dates)
                {
                    // Souscrire aux changements de la collection
                    dates.CollectionChanged += (s, args) =>
                    {
                        calendar.InvalidateVisual();
                    };
                }
            }
        }
    }
}
