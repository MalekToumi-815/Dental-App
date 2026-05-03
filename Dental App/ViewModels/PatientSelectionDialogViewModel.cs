using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Dental_App.Models;
using Dental_App.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;

namespace Dental_App.ViewModels
{
    public class PatientSelectionDialogViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly ILiveSearchService<Patient> _liveSearchService;
        private string _searchTerm = string.Empty;
        private ObservableCollection<PatientDisplayModel> _filteredPatients;
        private PatientDisplayModel _selectedPatient;
        private DelegateCommand _selectCommand;
        private DelegateCommand _cancelCommand;
        private bool _isPatientSelected;
        private bool _isNoResultsVisible;
        private bool _isLoadingPatients = true;

        public PatientSelectionDialogViewModel(IPatientService patientService, ILiveSearchService<Patient> liveSearchService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));
            _filteredPatients = new ObservableCollection<PatientDisplayModel>();
            
            SelectCommand = new DelegateCommand(ExecuteSelectCommand, CanExecuteSelectCommand).ObservesProperty(() => IsPatientSelected);
            CancelCommand = new DelegateCommand(ExecuteCancelCommand);
            
            _ = LoadAllPatientsAsync();
        }

        public string Title => "Sélectionner un Patient";
        
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    _ = SearchPatientsAsync(value);
                }
            }
        }

        public ObservableCollection<PatientDisplayModel> FilteredPatients
        {
            get => _filteredPatients;
            set => SetProperty(ref _filteredPatients, value);
        }

        public PatientDisplayModel SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    IsPatientSelected = value != null;
                }
            }
        }

        public bool IsPatientSelected
        {
            get => _isPatientSelected;
            set => SetProperty(ref _isPatientSelected, value);
        }

        public bool IsNoResultsVisible
        {
            get => _isNoResultsVisible;
            set => SetProperty(ref _isNoResultsVisible, value);
        }

        public bool IsLoadingPatients
        {
            get => _isLoadingPatients;
            set => SetProperty(ref _isLoadingPatients, value);
        }

        public string PatientCount
        {
            get => $"{FilteredPatients.Count} patient(s)";
        }

        public DelegateCommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        public DelegateCommand CancelCommand
        {
            get => _cancelCommand;
            set => SetProperty(ref _cancelCommand, value);
        }

        public Action<PatientSelectionResult> CloseDialog { get; set; }

        private async Task LoadAllPatientsAsync()
        {
            try
            {
                IsLoadingPatients = true;
                var patients = await _patientService.GetAllAsync();
                FilteredPatients.Clear();
                
                foreach (var patient in patients)
                {
                    FilteredPatients.Add(new PatientDisplayModel(patient));
                }

                UpdateNoResultsVisibility();
                System.Diagnostics.Debug.WriteLine($"[PatientSelectionDialog] Loaded {FilteredPatients.Count} patients");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PatientSelectionDialog] Error loading patients: {ex.Message}");
            }
            finally
            {
                IsLoadingPatients = false;
            }
        }

        private async Task SearchPatientsAsync(string searchTerm)
        {
            try
            {
                IsLoadingPatients = true;

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    await LoadAllPatientsAsync();
                    return;
                }

                var results = await _liveSearchService.SearchAsync(searchTerm.Trim(), async (term) => 
                    await _patientService.SearchByNameAsync(term, 50));

                if (results != null)
                {
                    FilteredPatients.Clear();
                    foreach (var patient in results)
                    {
                        FilteredPatients.Add(new PatientDisplayModel(patient));
                    }

                    UpdateNoResultsVisibility();
                    RaisePropertyChanged(nameof(PatientCount));
                    System.Diagnostics.Debug.WriteLine($"[PatientSelectionDialog] Search '{searchTerm}' returned {FilteredPatients.Count} results");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PatientSelectionDialog] Error searching patients: {ex.Message}");
            }
            finally
            {
                IsLoadingPatients = false;
            }
        }

        private void UpdateNoResultsVisibility()
        {
            IsNoResultsVisible = FilteredPatients.Count == 0 && !string.IsNullOrWhiteSpace(SearchTerm);
        }

        private void ExecuteSelectCommand()
        {
            if (SelectedPatient == null)
            {
                System.Diagnostics.Debug.WriteLine("[PatientSelectionDialog] Select command executed but no patient selected");
                return;
            }

            var result = new PatientSelectionResult
            {
                PatientId = SelectedPatient.Id,
                PatientName = SelectedPatient.FullName,
                Patient = SelectedPatient.ToPatient()
            };
            
            System.Diagnostics.Debug.WriteLine($"[PatientSelectionDialog] Patient selected: {result.PatientName} (ID: {result.PatientId})");
            CloseDialog?.Invoke(result);
        }

        private bool CanExecuteSelectCommand()
        {
            return SelectedPatient != null;
        }

        private void ExecuteCancelCommand()
        {
            System.Diagnostics.Debug.WriteLine("[PatientSelectionDialog] Cancel command executed");
            CloseDialog?.Invoke(null);
        }
    }

    /// <summary>
    /// Display model for a patient in the selection dialog
    /// </summary>
    public class PatientDisplayModel
    {
        private readonly Patient _patient;

        public PatientDisplayModel(Patient patient)
        {
            _patient = patient ?? throw new ArgumentNullException(nameof(patient));
        }

        public int Id => _patient.Id;
        public string Nom => _patient.Nom;
        public string Prenom => _patient.Prenom;
        public string FullName => $"{_patient.Prenom} {_patient.Nom}";
        public string Telephone => _patient.Telephone;
        public DateOnly DateNaissance => _patient.DateNaissance;
        public string Adresse => _patient.Adresse;

        public Patient ToPatient() => _patient;
    }

    /// <summary>
    /// Result from patient selection dialog
    /// </summary>
    public class PatientSelectionResult
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public Patient Patient { get; set; }
    }
}
