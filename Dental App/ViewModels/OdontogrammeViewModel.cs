using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Windows;
using Dental_App.Views;
using Dental_App.Services;
using Prism.Ioc;

namespace Dental_App.ViewModels
{
    public class OdontogrammeViewModel : BindableBase
    {
        private string _patientInfo = "Aucun patient sélectionné";
        private DelegateCommand _choisirPatientCommand;
        private int? _selectedPatientId;
        private string _selectedPatientName;
        private readonly IPatientService _patientService;

        public OdontogrammeViewModel(IPatientService patientService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            ChoisirPatientCommand = new DelegateCommand(ExecuteChoisirPatient);
        }

        public string PatientInfo
        {
            get => _patientInfo;
            set => SetProperty(ref _patientInfo, value);
        }

        public int? SelectedPatientId
        {
            get => _selectedPatientId;
            set => SetProperty(ref _selectedPatientId, value);
        }

        public string SelectedPatientName
        {
            get => _selectedPatientName;
            set => SetProperty(ref _selectedPatientName, value);
        }

        public DelegateCommand ChoisirPatientCommand
        {
            get => _choisirPatientCommand;
            set => SetProperty(ref _choisirPatientCommand, value);
        }

        private void ExecuteChoisirPatient()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[OdontogrammeViewModel] Opening patient selection dialog...");

                var dialogViewModel = new PatientSelectionDialogViewModel(_patientService);
                var dialogView = new PatientSelectionDialogView { DataContext = dialogViewModel };

                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10),
                    ResizeMode = ResizeMode.NoResize
                };

                dialogViewModel.CloseDialog = (result) =>
                {
                    if (result != null && result.PatientId > 0)
                    {
                        SelectedPatientId = result.PatientId;
                        SelectedPatientName = result.PatientName;
                        PatientInfo = $"Patient: {result.PatientName} (ID: {result.PatientId})";
                        
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] ? Patient selected successfully");
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] - Name: {result.PatientName}");
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] - ID: {result.PatientId}");
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] - Patient object available: {(result.Patient != null ? "Yes" : "No")}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[OdontogrammeViewModel] Patient selection cancelled or result is null");
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] ? Error in ExecuteChoisirPatient: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Erreur lors de l'ouverture du dialogue:\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
