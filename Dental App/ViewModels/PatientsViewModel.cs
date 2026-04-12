using Dental_App.Models;
using Dental_App.Services;
using Dental_App.Views;
using Dental_App.ViewModels;
using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class PatientsViewModel : BindableBase
    {
        private readonly IPatientService _patientService;

        private ObservableCollection<PatientDisplayRow> _patients;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private PatientDisplayRow _selectedPatient;
        private DelegateCommand _addPatientCommand;
        private DelegateCommand<PatientDisplayRow> _viewPatientCommand;
        private DelegateCommand<PatientDisplayRow> _editPatientCommand;

        public PatientsViewModel(IPatientService patientService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            Patients = new ObservableCollection<PatientDisplayRow>();
            AddPatientCommand = new DelegateCommand(AddPatient);
            ViewPatientCommand = new DelegateCommand<PatientDisplayRow>(ViewPatient);
            EditPatientCommand = new DelegateCommand<PatientDisplayRow>(EditPatient);

            System.Diagnostics.Debug.WriteLine("PatientsViewModel: Constructor called");
            _ = LoadPatientsAsync();
        }

        public ObservableCollection<PatientDisplayRow> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
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

        public PatientDisplayRow SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        public DelegateCommand AddPatientCommand
        {
            get => _addPatientCommand;
            set => SetProperty(ref _addPatientCommand, value);
        }

        public DelegateCommand<PatientDisplayRow> ViewPatientCommand
        {
            get => _viewPatientCommand;
            set => SetProperty(ref _viewPatientCommand, value);
        }

        public DelegateCommand<PatientDisplayRow> EditPatientCommand
        {
            get => _editPatientCommand;
            set => SetProperty(ref _editPatientCommand, value);
        }

        private void AddPatient()
        {
            System.Diagnostics.Debug.WriteLine("PatientsViewModel: AddPatient command executed");
            
            try
            {
                var dialogViewModel = new AddPatientDialogViewModel(_patientService);
                var dialogView = new AddPatientDialogView { DataContext = dialogViewModel };

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
                        System.Diagnostics.Debug.WriteLine("PatientsViewModel: AddPatientDialog closed with OK");
                        _ = LoadPatientsAsync();
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddPatient: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPatient(PatientDisplayRow patient)
        {
            if (patient?.Patient == null)
                return;

            System.Diagnostics.Debug.WriteLine($"PatientsViewModel: ViewPatient command executed for patient {patient.Id}");
            
            try
            {
                var dialogViewModel = new PatientDetailsDialogViewModel(patient.Patient);
                // Set the financial data
                dialogViewModel.TotalAmount = patient.RemainingAmountValue + (patient.Patient.SommePaye ?? 0);
                dialogViewModel.PaidAmount = patient.Patient.SommePaye ?? 0;
                dialogViewModel.RemainingAmount = patient.RemainingAmountValue;

                var dialogView = new PatientDetailsDialogView { DataContext = dialogViewModel };

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
                        System.Diagnostics.Debug.WriteLine($"PatientsViewModel: PatientDetailsDialog closed with OK for patient {patient.Id}");
                        // TODO: Handle payment addition if needed
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ViewPatient: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditPatient(PatientDisplayRow patient)
        {
            if (patient?.Patient == null)
                return;

            System.Diagnostics.Debug.WriteLine($"PatientsViewModel: EditPatient command executed for patient {patient.Id}");
            
            try
            {
                var dialogViewModel = new AddPatientDialogViewModel(_patientService, patient.Patient);
                var dialogView = new AddPatientDialogView { DataContext = dialogViewModel };

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
                        System.Diagnostics.Debug.WriteLine("PatientsViewModel: EditPatientDialog closed with OK");
                        _ = LoadPatientsAsync();
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EditPatient: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PatientsViewModel: LoadPatientsAsync started");
                IsLoading = true;
                var patients = await _patientService.GetAllAsync();
                System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Retrieved {patients?.Count ?? 0} patients from database");
                await BuildPatientRows(patients);
                System.Diagnostics.Debug.WriteLine($"PatientsViewModel: LoadPatientsAsync completed, Patients count: {Patients?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Error loading patients: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Erreur lors du chargement des patients: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformSearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadPatientsAsync();
                    return;
                }

                IsLoading = true;
                var searchResults = await _patientService.SearchByNameAsync(SearchText.Trim(), maxResults: 100);
                await BuildPatientRows(searchResults);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching patients: {ex.Message}");
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BuildPatientRows(List<Patient> patients)
        {
            System.Diagnostics.Debug.WriteLine($"PatientsViewModel: BuildPatientRows started with {patients?.Count ?? 0} patients");
            
            if (patients == null || patients.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("PatientsViewModel: No patients to build rows from");
                Patients = new ObservableCollection<PatientDisplayRow>();
                System.Diagnostics.Debug.WriteLine($"PatientsViewModel: BuildPatientRows completed with 0 rows");
                return;
            }

            var rows = new ObservableCollection<PatientDisplayRow>();

            // Build all rows with error handling for each patient
            foreach (var patient in patients)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Processing patient {patient.Id}: {patient.Prenom} {patient.Nom}");
                    
                    var totalAmount = await _patientService.GetSommeAPayerAsync(patient.Id);
                    var paidAmount = patient.SommePaye ?? 0m;
                    var remainingAmount = totalAmount - paidAmount;

                    // Get last consultation date
                    string lastConsultationDate = "-";
                    try
                    {
                        var patientWithConsultations = await _patientService.GetByIdWithConsultationsAsync(patient.Id);
                        if (patientWithConsultations?.Consultations != null && patientWithConsultations.Consultations.Count > 0)
                        {
                            var lastConsultation = patientWithConsultations.Consultations
                                .OrderByDescending(c => c.DateConsultation)
                                .FirstOrDefault();
                            if (lastConsultation?.DateConsultation.HasValue == true)
                            {
                                lastConsultationDate = lastConsultation.DateConsultation.Value.ToString("dd/MM/yyyy");
                            }
                        }
                    }
                    catch (Exception consultEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Error getting consultation for patient {patient.Id}: {consultEx.Message}");
                    }

                    var displayRow = new PatientDisplayRow
                    {
                        Id = patient.Id,
                        FullName = $"{patient.Prenom} {patient.Nom}",
                        Phone = patient.Telephone ?? string.Empty,
                        DateOfBirth = patient.DateNaissance.ToString("dd/MM/yyyy"),
                        LastConsultation = lastConsultationDate,
                        TotalAmount = $"{totalAmount:F0} DT",
                        PaidAmount = $"{paidAmount:F0} DT",
                        RemainingAmount = $"{remainingAmount:F0} DT",
                        RemainingAmountValue = remainingAmount,
                        Patient = patient
                    };
                    
                    rows.Add(displayRow);
                    System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Added row for patient {patient.Id}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Error building row for patient {patient.Id}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"PatientsViewModel: Stack trace: {ex.StackTrace}");
                    // Continue processing other patients even if one fails
                }
            }

            System.Diagnostics.Debug.WriteLine($"PatientsViewModel: BuildPatientRows completed with {rows.Count} rows");
            
            // Update the collection on the UI thread
            Patients = rows;
        }
    }

    public class PatientDisplayRow : BindableBase
    {
        private int _id;
        private string _fullName;
        private string _phone;
        private string _dateOfBirth;
        private string _lastConsultation;
        private string _totalAmount;
        private string _paidAmount;
        private string _remainingAmount;
        private decimal _remainingAmountValue;
        private Patient _patient;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string DateOfBirth
        {
            get => _dateOfBirth;
            set => SetProperty(ref _dateOfBirth, value);
        }

        public string LastConsultation
        {
            get => _lastConsultation;
            set => SetProperty(ref _lastConsultation, value);
        }

        public string TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public string PaidAmount
        {
            get => _paidAmount;
            set => SetProperty(ref _paidAmount, value);
        }

        public string RemainingAmount
        {
            get => _remainingAmount;
            set => SetProperty(ref _remainingAmount, value);
        }

        public decimal RemainingAmountValue
        {
            get => _remainingAmountValue;
            set => SetProperty(ref _remainingAmountValue, value);
        }

        public Patient Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }
    }
}
