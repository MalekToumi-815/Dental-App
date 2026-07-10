using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for SidebarView.xaml
    /// </summary>
    public partial class SidebarView : UserControl
    {
        private Dictionary<string, Button> _buttonMap;

        public SidebarView()
        {
            InitializeComponent();

            // Map view names to their corresponding buttons
            _buttonMap = new Dictionary<string, Button>
            {
                { "DashboardView", DashboardButton },
                { "RendezVousView", RendezVousButton },
                { "ConsultationView", ConsultationButton },
                { "PatientsView", PatientsButton },
                { "AntecedentView", AntecedentsButton }, // Fixed
                { "OrdonnanceView", OrdonnancesButton }, // Fixed
                { "ActesMedicauxView", ActesMedicauxButton },
                { "RadioImagesView", RadioImagesButton },
                { "OdontogrammeView", OdontogrammeButton },
                { "CaisseView", CaisseButton },
                { "EvolutionView", EvolutionButton }, // Added mapping so it highlights
                { "CommandeProthesisteView", CommandeProthesisteButton },
                { "ProthesisteView", ProthesistesButton } // Fixed
            };

            this.Loaded += SidebarView_Loaded;
        }

        private void SidebarView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SidebarViewModel viewModel)
            {
                // Initial setup
                UpdateActiveButtonStyle(viewModel.ActiveView);

                // Subscribe to ActiveView changes
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(SidebarViewModel.ActiveView))
                    {
                        UpdateActiveButtonStyle(viewModel.ActiveView);
                    }
                };
            }
        }

        private void UpdateActiveButtonStyle(string activeView)
        {
            // Reset all buttons to inactive style
            foreach (var button in _buttonMap.Values)
            {
                button.Style = (Style)this.Resources["NavButtonStyle"];
            }

            // Apply active style to the current view's button
            if (_buttonMap.TryGetValue(activeView, out var activeButton))
            {
                activeButton.Style = (Style)this.Resources["NavButtonActiveStyle"];
            }
        }

        private void AntecedentsButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
