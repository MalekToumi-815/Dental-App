using Dental_App.Models;
using Dental_App.Services;
using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // Keep if required for logic mappings
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
        private readonly IAppNotificationService _notificationService;

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

        // Pagination fields
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 10;
        private int _totalConsultationsCount = 0;
        private DelegateCommand _nextPageCommand;
        private DelegateCommand _previousPageCommand;

        public ConsultationViewModel(IPatientService patientService, IConsultationService consultationService, IDentService dentService, IActeMedicalService acteService, ILiveSearchService<Patient> liveSearchService, IAppNotificationService notificationService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _dentService = dentService ?? throw new ArgumentNullException(nameof(dentService));
            _acteService = acteService ?? throw new ArgumentNullException(nameof(acteService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            AllPatients = new ObservableCollection<PatientForConsultation>();
            FilteredPatients = new ObservableCollection<PatientForConsultation>();
            Consultations = new ObservableCollection<ConsultationDisplayRow>();

            AddConsultationCommand = new DelegateCommand(AddConsultation);
            EditConsultationCommand = new DelegateCommand<ConsultationDisplayRow>(EditConsultation);
            GererActeCommand = new DelegateCommand<ConsultationDisplayRow>(GererActe);

            // Pagination commands
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), () => CurrentPage > 1);

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

                    // Reset paging when patient changes
                    CurrentPage = 1;

                    // Load first page for the selected patient
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

        // Use total count returned by the paged service instead of current page count
        public string ConsultationCountDisplay => _totalConsultationsCount == 0
            ? "0 consultation(s)"
            : $"{_totalConsultationsCount} consultation(s)";

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

        // Pagination public properties
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    // Ensure bounds
                    if (_currentPage < 1) _currentPage = 1;
                    if (TotalPages > 0 && _currentPage > TotalPages) _currentPage = TotalPages;

                    // Notify commands to re-evaluate
                    NextPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    NextPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    // When page size changes, reset to first page and reload
                    CurrentPage = 1;
                    _ = LoadCurrentPageAsync();
                }
            }
        }

        public DelegateCommand NextPageCommand
        {
            get => _nextPageCommand;
            set => SetProperty(ref _nextPageCommand, value);
        }

        public DelegateCommand PreviousPageCommand
        {
            get => _previousPageCommand;
            set => SetProperty(ref _previousPageCommand, value); // small fallback name correction
        }

        // Logic property for XAML Visibility
        public bool IsPatientSelected => SelectedPatient != null;

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                // Limit initial load to first 10 patients to improve performance in the patient selector
                List<Patient> patients = null;
                try
                {
                    // Use paged fetch exposed on IPatientService (used elsewhere in the app)
                    patients = await _patientService.GetPatientsAsync(1, 10);
                }
                catch
                {
                    // Fall back to GetAllAsync if paged method isn't available for some reason
                    patients = await _patientService.GetAllAsync();
                }

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
                _notificationService.ShowError($"Erreur chargement: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Publicly callable refresh method so the view can trigger reloading when it becomes visible.
        /// Reloads patient selector and current consultations page (if a patient is selected).
        /// </summary>
        public void Refresh()
        {
            // Fire-and-forget the async load operations; Refresh is called from the view events.
            _ = LoadPatientsAsync();
            if (SelectedPatient != null)
            {
                _ = LoadCurrentPageAsync();
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

        // This method is called when a patient is selected. It now triggers loading of the current page (paged fetch)
        private async Task OnPatientSelectedAsync(PatientForConsultation patient)
        {
            try
            {
                if (patient == null)
                {
                    Consultations.Clear();
                    HeaderSubtitle = "Sélectionnez un patient pour commencer";
                    _totalConsultationsCount = 0;
                    TotalPages = 1;
                    CurrentPage = 1;
                }
                else
                {
                    IsLoading = true;
                    HeaderSubtitle = $"Patient: {patient.FullName}";

                    await LoadCurrentPageAsync();
                }

                // Force refresh of all dependent strings
                RaisePropertyChanged(nameof(ConsultationCountDisplay));
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Loads consultations for the CurrentPage without changing CurrentPage
        private async Task LoadCurrentPageAsync()
        {
            if (SelectedPatient == null)
            {
                Consultations = new ObservableCollection<ConsultationDisplayRow>();
                _totalConsultationsCount = 0;
                TotalPages = 1;
                RaisePropertyChanged(nameof(ConsultationCountDisplay));
                return;
            }

            try
            {
                IsLoading = true;

                var (items, total) = await _consultationService.GetByPatientIdPagedAsync(SelectedPatient.Id, CurrentPage, PageSize);

                _totalConsultationsCount = total;
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

                // Ensure CurrentPage is within bounds
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    // reload for the adjusted page
                    var (newItems, _) = await _consultationService.GetByPatientIdPagedAsync(SelectedPatient.Id, CurrentPage, PageSize);
                    items = newItems;
                }

                var consultationRows = new ObservableCollection<ConsultationDisplayRow>();

                if (items != null)
                {
                    foreach (var c in items)
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
                RaisePropertyChanged(nameof(ConsultationCountDisplay));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading page: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                NextPageCommand?.RaiseCanExecuteChanged();
                PreviousPageCommand?.RaiseCanExecuteChanged();
            }
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadCurrentPageAsync();
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadCurrentPageAsync();
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
                var dialogViewModel = new ConsultationDialogViewModel(_consultationService, _dentService, _notificationService, SelectedPatient.Id);
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
                        _ = LoadCurrentPageAsync();
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddConsultation: {ex.Message}");
                _notificationService.ShowError($"Erreur: {ex.Message}");
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
                    _notificationService.ShowError("Erreur: Consultation non trouvee.");
                    return;
                }

                var dialogViewModel = new ConsultationDialogViewModel(_consultationService, _dentService, _notificationService, SelectedPatient.Id, consultationToEdit);
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
                        _ = LoadCurrentPageAsync();
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EditConsultation: {ex.Message}");
                _notificationService.ShowError($"Erreur: {ex.Message}");
            }
        }

        private void GererActe(ConsultationDisplayRow consultation)
        {
            if (consultation == null)
                return;

            System.Diagnostics.Debug.WriteLine($"ConsultationViewModel: GererActe command executed for consultation {consultation.Id}");

            try
            {
                var dialogViewModel = new GererActeDialogViewModel(_acteService, _consultationService, _notificationService, consultation.Id);
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
                        _ = LoadCurrentPageAsync();
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GererActe: {ex.Message}");
                _notificationService.ShowError($"Erreur: {ex.Message}");
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