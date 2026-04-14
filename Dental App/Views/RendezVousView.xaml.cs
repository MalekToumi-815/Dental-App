using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Dental_App.Models;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for RendezVousView.xaml
    /// </summary>
    public partial class RendezVousView : UserControl
    {
        private ToggleButton _lastSelectedTimeButton;

        public RendezVousView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gère le clic sur un rendez-vous pour l'éditer
        /// </summary>
        private void OnRendezVousClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is RendezVousItemViewModel rdv)
            {
                if (this.DataContext is RendezVousViewModel vm)
                {
                    vm.EditRendezVousCommand.Execute(rdv);
                }
            }
        }

        /// <summary>
        /// Gère le clic sur une suggestion de patient
        /// </summary>
        private void OnPatientSuggestionClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Patient patient)
            {
                if (this.DataContext is RendezVousViewModel vm)
                {
                    // Sélectionner le patient via le ViewModel de recherche
                    vm.PatientSearchViewModel.SelectedPatient = patient;
                }
            }
        }

        /// <summary>
        /// Gère la sélection des créneaux horaires avec une sélection exclusive
        /// </summary>
        private void OnTimeSlotClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                // Décocher le bouton précédemment sélectionné
                if (_lastSelectedTimeButton != null && _lastSelectedTimeButton != button)
                {
                    _lastSelectedTimeButton.IsChecked = false;
                }

                if (button.IsChecked == true)
                {
                    _lastSelectedTimeButton = button;
                    
                    // Mettre à jour le ViewModel avec l'heure sélectionnée
                    if (this.DataContext is RendezVousViewModel vm)
                    {
                        vm.SelectedTimeSlot = button.Tag?.ToString();
                    }
                }
                else
                {
                    _lastSelectedTimeButton = null;
                    
                    if (this.DataContext is RendezVousViewModel vm)
                    {
                        vm.SelectedTimeSlot = null;
                    }
                }
            }
        }

        /// <summary>
        /// Met à jour l'affichage des points indicateurs quand le mois change
        /// </summary>
        private void Calendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            // if (!(this.DataContext is RendezVousViewModel vm))
            //     return;

            // // Parcourir tous les CalendarDayButtons visibles et mettre à jour les points
            // UpdateAppointmentIndicators(MainCalendar, vm.DatesWithAppointments);
        }

        /// <summary>
        /// Met à jour les points indicateurs sur les jours avec rendez-vous
        /// </summary>
        private void UpdateAppointmentIndicators(Calendar calendar, ObservableCollection<DateTime> datesWithAppointments)
        {
            try
            {
                // Trouver tous les CalendarDayButtons dans le calendrier
                var buttons = FindVisualChildren<CalendarDayButton>(calendar);

                foreach (var button in buttons)
                {
                    // Obtenir la date du bouton
                    if (button.DataContext is DateTime buttonDate)
                    {
                        // Vérifier si cette date a un rendez-vous
                        bool hasAppointment = datesWithAppointments.Any(d => d.Date == buttonDate.Date);
                        
                        // Mettre à jour la visibilité du point indicateur
                        var appointmentDot = FindChild<Ellipse>(button, "AppointmentDot");
                        if (appointmentDot != null)
                        {
                            appointmentDot.Visibility = hasAppointment ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateAppointmentIndicators] Erreur: {ex.Message}");
            }
        }

        /// <summary>
        /// Trouve tous les éléments visuels enfants d'un type donné
        /// </summary>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        /// <summary>
        /// Trouve un élément enfant avec un nom donné
        /// </summary>
        private T FindChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && (typedChild as FrameworkElement)?.Name == name)
                    return typedChild;

                var result = FindChild<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
