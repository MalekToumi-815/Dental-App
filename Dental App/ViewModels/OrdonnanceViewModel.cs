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
    public class OrdonnanceViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IOrdonnanceService _ordonnanceService;

        private ObservableCollection<PatientForOrdonnance> _allPatients;
        private ObservableCollection<PatientForOrdonnance> _filteredPatients;
        private ObservableCollection<OrdonnanceDisplayRow> _ordonnances;
        private PatientForOrdonnance _selectedPatient;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _headerSubtitle = "Sélectionnez un patient pour commencer";
        private DelegateCommand _addOrdonnanceCommand;
        private DelegateCommand<OrdonnanceDisplayRow> _printOrdonnanceCommand;
        private DelegateCommand<OrdonnanceDisplayRow> _deleteOrdonnanceCommand;

        public OrdonnanceViewModel(IPatientService patientService, IOrdonnanceService ordonnanceService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _ordonnanceService = ordonnanceService ?? throw new ArgumentNullException(nameof(ordonnanceService));

            AllPatients = new ObservableCollection<PatientForOrdonnance>();
            FilteredPatients = new ObservableCollection<PatientForOrdonnance>();
            Ordonnances = new ObservableCollection<OrdonnanceDisplayRow>();

            AddOrdonnanceCommand = new DelegateCommand(AddOrdonnance);
            PrintOrdonnanceCommand = new DelegateCommand<OrdonnanceDisplayRow>(PrintOrdonnance);
            DeleteOrdonnanceCommand = new DelegateCommand<OrdonnanceDisplayRow>(DeleteOrdonnance);

            _ = LoadPatientsAsync();
        }

        public ObservableCollection<PatientForOrdonnance> AllPatients
        {
            get => _allPatients;
            set => SetProperty(ref _allPatients, value);
        }

        public ObservableCollection<PatientForOrdonnance> FilteredPatients
        {
            get => _filteredPatients;
            set => SetProperty(ref _filteredPatients, value);
        }

        public ObservableCollection<OrdonnanceDisplayRow> Ordonnances
        {
            get => _ordonnances;
            set => SetProperty(ref _ordonnances, value);
        }

        public PatientForOrdonnance SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
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
                    PerformSearch();
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

        public string OrdonnanceCountDisplay => Ordonnances == null || Ordonnances.Count == 0
            ? "0 ordonnance(s)"
            : $"{Ordonnances.Count} ordonnance(s)";

        public DelegateCommand AddOrdonnanceCommand
        {
            get => _addOrdonnanceCommand;
            set => SetProperty(ref _addOrdonnanceCommand, value);
        }

        public DelegateCommand<OrdonnanceDisplayRow> PrintOrdonnanceCommand
        {
            get => _printOrdonnanceCommand;
            set => SetProperty(ref _printOrdonnanceCommand, value);
        }

        public DelegateCommand<OrdonnanceDisplayRow> DeleteOrdonnanceCommand
        {
            get => _deleteOrdonnanceCommand;
            set => SetProperty(ref _deleteOrdonnanceCommand, value);
        }

        public bool IsPatientSelected => SelectedPatient != null;

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                var patients = await _patientService.GetAllAsync();

                var patientList = new ObservableCollection<PatientForOrdonnance>();
                if (patients != null)
                {
                    foreach (var patient in patients)
                    {
                        patientList.Add(new PatientForOrdonnance
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
                FilteredPatients = new ObservableCollection<PatientForOrdonnance>(AllPatients);
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

        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredPatients = new ObservableCollection<PatientForOrdonnance>(AllPatients);
                return;
            }

            var searchTerm = SearchText.Trim().ToLower();
            var filtered = AllPatients
                .Where(p => p.FullName.ToLower().Contains(searchTerm) ||
                           p.Telephone.ToLower().Contains(searchTerm))
                .ToList();

            FilteredPatients = new ObservableCollection<PatientForOrdonnance>(filtered);
        }

        private async Task OnPatientSelectedAsync(PatientForOrdonnance patient)
        {
            try
            {
                if (patient == null)
                {
                    Ordonnances.Clear();
                    HeaderSubtitle = "Sélectionnez un patient pour commencer";
                }
                else
                {
                    IsLoading = true;
                    var ordonnances = await _ordonnanceService.GetByPatientIdAsync(patient.Id);
                    var ordonnancesRows = new ObservableCollection<OrdonnanceDisplayRow>();

                    if (ordonnances != null)
                    {
                        foreach (var o in ordonnances)
                        {
                            ordonnancesRows.Add(new OrdonnanceDisplayRow
                            {
                                Id = o.Id,
                                Date = o.DateCreation ?? DateTime.Now,
                                MedicamentsCount = o.Medicaments?.Count ?? 0
                            });
                        }
                    }

                    Ordonnances = ordonnancesRows;
                    HeaderSubtitle = $"Patient: {patient.FullName}";
                }

                RaisePropertyChanged(nameof(OrdonnanceCountDisplay));
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

        private void AddOrdonnance()
        {
            if (SelectedPatient == null)
                return;

            try
            {
                var dialogViewModel = new OrdonnanceDialogViewModel(_ordonnanceService, SelectedPatient.Id, SelectedPatient.FullName);
                var dialogView = new OrdonnanceDialogView { DataContext = dialogViewModel };

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
                    if (result == true)
                    {
                        _ = OnPatientSelectedAsync(SelectedPatient);
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintOrdonnance(OrdonnanceDisplayRow ordonnance)
        {
            if (ordonnance == null)
                return;

            // Empty placeholder for functionality
            MessageBox.Show($"Printing ordonnance {ordonnance.Id}");
        }

        private void DeleteOrdonnance(OrdonnanceDisplayRow ordonnance)
        {
            if (ordonnance == null)
                return;

            // Empty placeholder for functionality
            MessageBox.Show($"Deleting ordonnance {ordonnance.Id}");
        }
    }

    public class PatientForOrdonnance : BindableBase
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Telephone { get; set; }
        public string Initials { get; set; }
        public Patient Patient { get; set; }
    }

    public class OrdonnanceDisplayRow : BindableBase
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        
        private int _medicamentsCount;
        public int MedicamentsCount 
        { 
            get => _medicamentsCount;
            set => SetProperty(ref _medicamentsCount, value);
        }
    }
}
