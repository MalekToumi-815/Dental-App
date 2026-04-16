using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Dental_App.Views;
using Dental_App.Services;
using System.Linq;

namespace Dental_App.ViewModels
{
    public class OdontogrammeViewModel : BindableBase
    {
        private string _patientInfo = "Aucun patient sélectionné";
        private DelegateCommand _choisirPatientCommand;
        private DelegateCommand<string> _toothClickedCommand;
        private DelegateCommand _toggleViewModeCommand;
        private int? _selectedPatientId;
        private string _selectedPatientName;
        private readonly IPatientService _patientService;
        private readonly IDentService _dentService;
        private ObservableCollection<ToothActDisplayItem> _actsHistory;
        private string _selectedToothInfo = "";
        private bool _isNoActsMessage = true;
        private bool _isNoActsFound = false;
        private string _noToothSelectedMessage = "Cliquez sur une dent pour voir l'historique des actes";
        private bool _isHistoryMode = true;

        public OdontogrammeViewModel(IPatientService patientService, IDentService dentService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _dentService = dentService ?? throw new ArgumentNullException(nameof(dentService));
            _actsHistory = new ObservableCollection<ToothActDisplayItem>();
            
            ChoisirPatientCommand = new DelegateCommand(ExecuteChoisirPatient);
            ToothClickedCommand = new DelegateCommand<string>(ExecuteToothClicked);
            ToggleViewModeCommand = new DelegateCommand(ExecuteToggleViewMode);
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

        public DelegateCommand<string> ToothClickedCommand
        {
            get => _toothClickedCommand;
            set => SetProperty(ref _toothClickedCommand, value);
        }

        public DelegateCommand ToggleViewModeCommand
        {
            get => _toggleViewModeCommand;
            set => SetProperty(ref _toggleViewModeCommand, value);
        }

        public ObservableCollection<ToothActDisplayItem> ActsHistory
        {
            get => _actsHistory;
            set => SetProperty(ref _actsHistory, value);
        }

        public string SelectedToothInfo
        {
            get => _selectedToothInfo;
            set => SetProperty(ref _selectedToothInfo, value);
        }

        public bool IsNoActsMessage
        {
            get => _isNoActsMessage;
            set => SetProperty(ref _isNoActsMessage, value);
        }

        public bool IsNoActsFound
        {
            get => _isNoActsFound;
            set => SetProperty(ref _isNoActsFound, value);
        }

        public string NoToothSelectedMessage
        {
            get => _noToothSelectedMessage;
            set => SetProperty(ref _noToothSelectedMessage, value);
        }

        public bool IsHistoryMode
        {
            get => _isHistoryMode;
            set => SetProperty(ref _isHistoryMode, value);
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
                        ClearActsHistory();
                        
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] ? Patient selected successfully");
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] - Name: {result.PatientName}");
                        System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] - ID: {result.PatientId}");
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
                MessageBox.Show($"Erreur lors de l'ouverture du dialogue:\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteToothClicked(string fdiCode)
        {
            if (!SelectedPatientId.HasValue || SelectedPatientId.Value <= 0)
            {
                System.Diagnostics.Debug.WriteLine("[OdontogrammeViewModel] No patient selected. Please select a patient first.");
                MessageBox.Show("Veuillez d'abord sélectionner un patient.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(fdiCode) || !int.TryParse(fdiCode, out int parsedFdiCode))
            {
                System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] Invalid FDI code: {fdiCode}");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] Tooth clicked: FDI={parsedFdiCode}, PatientId={SelectedPatientId}");

                var actsData = await _dentService.GetActesByPatientAndFdiAsync(SelectedPatientId.Value, parsedFdiCode);

                if (actsData == null || actsData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] No acts found for tooth {parsedFdiCode}");
                    SelectedToothInfo = $"Dent FDI: {parsedFdiCode}";
                    NoToothSelectedMessage = "Aucun acte trouvé pour cette dent";
                    IsNoActsMessage = true;
                    IsNoActsFound = true;
                    ActsHistory.Clear();
                    return;
                }

                SelectedToothInfo = $"Dent FDI: {parsedFdiCode} - {actsData.Count} consultation(s)";
                IsNoActsMessage = false;
                IsNoActsFound = false;
                
                ActsHistory.Clear();
                foreach (var history in actsData)
                {
                    ActsHistory.Add(new ToothActDisplayItem
                    {
                        ConsultationDate = history.ConsultationDate,
                        DateFormatted = history.ConsultationDate.ToString("dd/MM/yyyy"),
                        Actes = history.Actes?.Select(a => a.Libelle).ToList() ?? new System.Collections.Generic.List<string>(),
                        Notes = history.Notes ?? "Aucune note"
                    });
                }

                System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] ? Loaded {ActsHistory.Count} records for tooth {parsedFdiCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] ? Error loading acts: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des actes:\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                ClearActsHistory();
            }
        }

        private void ExecuteToggleViewMode()
        {
            IsHistoryMode = !IsHistoryMode;
            System.Diagnostics.Debug.WriteLine($"[OdontogrammeViewModel] View mode toggled: {(IsHistoryMode ? "History" : "Edit")}");
        }

        private void ClearActsHistory()
        {
            ActsHistory.Clear();
            SelectedToothInfo = "";
            IsNoActsMessage = true;
            IsNoActsFound = false;
            NoToothSelectedMessage = "Cliquez sur une dent pour voir l'historique des actes";
        }
    }

    /// <summary>
    /// Display item for tooth acts in the UI
    /// </summary>
    public class ToothActDisplayItem
    {
        public DateTime ConsultationDate { get; set; }
        public string DateFormatted { get; set; } = "";
        public List<string> Actes { get; set; } = new List<string>();
        public string Notes { get; set; } = "";
    }
}
