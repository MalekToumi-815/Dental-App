using Dental_App.Models;
using Dental_App.Services;
using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Dental_App.Views;

namespace Dental_App.ViewModels
{
    public class ConsultationViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IConsultationService _consultationService;
        private readonly IDentService _dentService;
        private readonly IActeMedicalService _acteService;
        private readonly ILiveSearchService<Patient> _liveSearchService;

        private ObservableCollection<PatientForConsultation> _allPatients;
        private ObservableCollection<PatientForConsultation> _filteredPatients;
        private ObservableCollection<ConsultationDisplayRow> _consultations;
        private PatientForConsultation _selectedPatient;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _headerSubtitle = "Sélectionnez un patient pour commencer";
        private DelegateCommand _addConsultationCommand;
        private DelegateCommand<ConsultationDisplayRow> _editConsultationCommand;
        private DelegateCommand<ConsultationDisplayRow> _gererActeCommand;

        public ConsultationViewModel(IPatientService patientService, IConsultationService consultationService, IDentService dentService, IActeMedicalService acteService, ILiveSearchService<Patient> liveSearchService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _dentService = dentService ?? throw new ArgumentNullException(nameof(dentService));
            _acteService = acteService ?? throw new ArgumentNullException(nameof(acteService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));

            AllPatients = new ObservableCollection<PatientForConsultation>();
            FilteredPatients = new ObservableCollection<PatientForConsultation>();
            Consultations = new ObservableCollection<ConsultationDisplayRow>();

            AddConsultationCommand = new DelegateCommand(AddConsultation);
            EditConsultationCommand = new DelegateCommand<ConsultationDisplayRow>(EditConsultation);
            GererActeCommand = new DelegateCommand<ConsultationDisplayRow>(GererActe);

            _ = LoadPatientsAsync();
        }

        public ObservableCollection<PatientForConsultation> AllPatients
        {
            get => _allPatients;
            set => SetProperty(ref _allPatients, value);
        }

        public ObservableCollection<PatientForConsultation> FilteredPatients
        {
            get => _filteredPatients;
            set => SetProperty(ref _filteredPatients, value);
        }

        public ObservableCollection<ConsultationDisplayRow> Consultations
        {
            get => _consultations;
            set => SetProperty(ref _consultations, value);
        }

        public PatientForConsultation SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    // Trigger UI updates for visibility logic
                    RaisePropertyChanged(nameof(IsPatientSelected));
                    _ = OnPatientSelectedAsync(value);
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = PerformSearchAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string HeaderSubtitle
        {
            get => _headerSubtitle;
            set => SetProperty(ref _headerSubtitle, value);
        }

        public string ConsultationCountDisplay => Consultations == null || Consultations.Count == 0
            ? "0 consultation(s)"
            : $"{Consultations.Count} consultation(s)";

        public DelegateCommand AddConsultationCommand
        {
            get => _addConsultationCommand;
            set => SetProperty(ref _addConsultationCommand, value);
        }

        public DelegateCommand<ConsultationDisplayRow> EditConsultationCommand
        {
            get => _editConsultationCommand;
            set => SetProperty(ref _editConsultationCommand, value);
        }

        public DelegateCommand<ConsultationDisplayRow> GererActeCommand
        {
            get => _gererActeCommand;
            set => SetProperty(ref _gererActeCommand, value);
        }

        // Logic property for XAML Visibility
        public bool IsPatientSelected => SelectedPatient != null;

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                var patients = await _patientService.GetAllAsync();

                var patientList = new ObservableCollection<PatientForConsultation>();
                if (patients != null)
                {
                    foreach (var patient in patients)
                    {
                        patientList.Add(new PatientForConsultation
                        {
                            Id = patient.Id,
                            FullName = $"{patient.Prenom} {patient.Nom}",
                            FirstName = patient.Prenom,
                            LastName = patient.Nom,
                            Telephone = patient.Telephone ?? string.Empty,
                            Initials = GetInitials(patient.Prenom, patient.Nom),
                            Patient = patient
                        });
                    }
                }

                AllPatients = patientList;
                FilteredPatients = new ObservableCollection<PatientForConsultation>(AllPatients);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadPatientsAsync();
                return;
            }

            IsLoading = true;
            try
            {
                var query = SearchText.Trim();
                var results = await _liveSearchService.SearchAsync(query, async (term) => 
                    await _patientService.SearchByNameAsync(term));

                if (results != null)
                {
                    var filtered = new ObservableCollection<PatientForConsultation>();
                    foreach (var p in results)
                    {
                        filtered.Add(new PatientForConsultation
                        {
                            Id = p.Id,
                            FullName = $"{p.Prenom} {p.Nom}",
                            FirstName = p.Prenom,
                            LastName = p.Nom,
                            Telephone = p.Telephone ?? string.Empty,
                            Initials = GetInitials(p.Prenom, p.Nom),
                            Patient = p
                        });
                    }
                    FilteredPatients = filtered;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error live search: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnPatientSelectedAsync(PatientForConsultation patient)
        {
            try
            {
                if (patient == null)
                {
                    Consultations.Clear();
                    HeaderSubtitle = "Sélectionnez un patient pour commencer";
                }
                else
                {
                    IsLoading = true;
                    var consultations = await _consultationService.GetByPatientIdAsync(patient.Id);
                    var consultationRows = new ObservableCollection<ConsultationDisplayRow>();

                    if (consultations != null)
                    {
                        foreach (var c in consultations)
                        {
                            consultationRows.Add(new ConsultationDisplayRow
                            {
                                Id = c.Id,
                                Date = c.DateConsultation ?? DateTime.Now,
                                Tooth = c.IdDentNavigation?.CodeFdi.ToString() ?? "-",
                                ActeName = c.IdActes?.Count > 0 ? string.Join(", ", c.IdActes.Select(a => a.Libelle)) : "-",
                                Amount = c.MontantTotal ?? 0,
                                Notes = c.Note ?? string.Empty
                            });
                        }
                    }

                    Consultations = consultationRows;
                    HeaderSubtitle = $"Patient: {patient.FullName}";
                }

                // Force refresh of all dependent strings
                RaisePropertyChanged(nameof(ConsultationCountDisplay));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetInitials(string firstName, string lastName)
        {
            var first = string.IsNullOrWhiteSpace(firstName) ? "?" : firstName[0].ToString().ToUpper();
            var last = string.IsNullOrWhiteSpace(lastName) ? "?" : lastName[0].ToString().ToUpper();
            return $"{first}{last}";
        }

        private void AddConsultation()
        {
            if (SelectedPatient == null)
                return;

            System.Diagnostics.Debug.WriteLine($"ConsultationViewModel: AddConsultation command executed for patient {SelectedPatient.Id}");

            try
            {
                var dialogViewModel = new ConsultationDialogViewModel(_consultationService, _dentService, SelectedPatient.Id);
                var dialogView = new ConsultationDialogView { DataContext = dialogViewModel };

                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                dialogViewModel.CloseDialog = (result) =>
                {
                    if (result != null && result == true)
                    {
                        System.Diagnostics.Debug.WriteLine("ConsultationViewModel: AddConsultationDialog closed with OK");
                        _ = OnPatientSelectedAsync(SelectedPatient);
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddConsultation: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditConsultation(ConsultationDisplayRow consultation)
        {
            if (consultation == null || SelectedPatient == null)
                return;

            System.Diagnostics.Debug.WriteLine($"ConsultationViewModel: EditConsultation command executed for consultation {consultation.Id}");

            try
            {
                // Fetch the full Consultation object from the database with all navigation properties
                var consultationToEdit = await _consultationService.GetByIdAsync(consultation.Id);
                
                if (consultationToEdit == null)
                {
                    MessageBox.Show($"Erreur: Consultation non trouvée.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialogViewModel = new ConsultationDialogViewModel(_consultationService, _dentService, SelectedPatient.Id, consultationToEdit);
                var dialogView = new ConsultationDialogView { DataContext = dialogViewModel };

                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                dialogViewModel.CloseDialog = (result) =>
                {
                    if (result != null && result == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"ConsultationViewModel: EditConsultationDialog closed with OK for consultation {consultation.Id}");
                        _ = OnPatientSelectedAsync(SelectedPatient);
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EditConsultation: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GererActe(ConsultationDisplayRow consultation)
        {
            if (consultation == null)
                return;

            System.Diagnostics.Debug.WriteLine($"ConsultationViewModel: GererActe command executed for consultation {consultation.Id}");

            try
            {
                var dialogViewModel = new GererActeDialogViewModel(_acteService, _consultationService, consultation.Id);
                var dialogView = new GererActeDialogView { DataContext = dialogViewModel };

                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                dialogViewModel.CloseDialog = (result) =>
                {
                    if (result != null && result == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"ConsultationViewModel: GererActeDialog closed with OK for consultation {consultation.Id}");
                        _ = OnPatientSelectedAsync(SelectedPatient);
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GererActe: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Model Wrapper Classes
    public class PatientForConsultation : BindableBase
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Telephone { get; set; }
        public string Initials { get; set; }
        public Patient Patient { get; set; }
    }

    public class ConsultationDisplayRow : BindableBase
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Tooth { get; set; }
        private string _acteName;
        public string ActeName 
        { 
            get => _acteName;
            set => SetProperty(ref _acteName, value);
        }
        public decimal Amount { get; set; }
        public string Notes { get; set; }

        /// <summary>
        /// Returns acts as a list, splitting by comma for vertical display
        /// </summary>
        public List<string> ActeNameList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ActeName) || ActeName == "-")
                    return new List<string> { "-" };
                
                return ActeName
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .ToList();
            }
        }
    }
}