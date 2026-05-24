using Dental_App.Models;
using Dental_App.Services;
using Dental_App.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dental_App.ViewModels
{
    public class OrdonnanceViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IOrdonnanceService _ordonnanceService;
        private readonly IOrdonnanceServiceTemplate _templateService;
        private readonly ILiveSearchService<Patient> _liveSearchService;
        private readonly IAppNotificationService _notificationService; // Add notification service

        private ObservableCollection<PatientForOrdonnance> _allPatients;
        private ObservableCollection<PatientForOrdonnance> _filteredPatients;
        private ObservableCollection<OrdonnanceDisplayRow> _ordonnances;
        private PatientForOrdonnance _selectedPatient;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _headerSubtitle = "Sélectionnez un patient pour commencer";
        private DelegateCommand _addOrdonnanceCommand;
        private DelegateCommand _addTemplateCommand;
        private DelegateCommand<OrdonnanceDisplayRow> _printOrdonnanceCommand;
        private DelegateCommand<OrdonnanceDisplayRow> _viewOrdonnanceCommand;

        public OrdonnanceViewModel(IPatientService patientService, IOrdonnanceService ordonnanceService, IOrdonnanceServiceTemplate templateService, ILiveSearchService<Patient> liveSearchService, IAppNotificationService notificationService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _ordonnanceService = ordonnanceService ?? throw new ArgumentNullException(nameof(ordonnanceService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService)); // Initialize notification service

            AllPatients = new ObservableCollection<PatientForOrdonnance>();
            FilteredPatients = new ObservableCollection<PatientForOrdonnance>();
            Ordonnances = new ObservableCollection<OrdonnanceDisplayRow>();

            AddOrdonnanceCommand = new DelegateCommand(AddOrdonnance);
            AddTemplateCommand = new DelegateCommand(AddTemplate);
            PrintOrdonnanceCommand = new DelegateCommand<OrdonnanceDisplayRow>(PrintOrdonnance);
            ViewOrdonnanceCommand = new DelegateCommand<OrdonnanceDisplayRow>(ViewOrdonnance);

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

        public string OrdonnanceCountDisplay => Ordonnances == null || Ordonnances.Count == 0
            ? "0 ordonnance(s)"
            : $"{Ordonnances.Count} ordonnance(s)";

        public DelegateCommand AddOrdonnanceCommand
        {
            get => _addOrdonnanceCommand;
            set => SetProperty(ref _addOrdonnanceCommand, value);
        }

        public DelegateCommand AddTemplateCommand
        {
            get => _addTemplateCommand;
            set => SetProperty(ref _addTemplateCommand, value);
        }

        public DelegateCommand<OrdonnanceDisplayRow> PrintOrdonnanceCommand
        {
            get => _printOrdonnanceCommand;
            set => SetProperty(ref _printOrdonnanceCommand, value);
        }

        public DelegateCommand<OrdonnanceDisplayRow> ViewOrdonnanceCommand
        {
            get => _viewOrdonnanceCommand;
            set => SetProperty(ref _viewOrdonnanceCommand, value);
        }

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
                    var filtered = new ObservableCollection<PatientForOrdonnance>();
                    foreach (var p in results)
                    {
                        filtered.Add(new PatientForOrdonnance
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

        private async Task OnPatientSelectedAsync(PatientForOrdonnance patient)
        {
            try
            {
                if (patient == null)
                {
                    Ordonnances.Clear();
                    HeaderSubtitle = "S\u00e9lectionnez un patient pour commencer";
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
                                MedicamentsCount = o.Medicaments?.Count ?? 0,
                                OrdonnanceObj = o
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

        /// <summary>
        /// Public refresh entry point so the view can request a data reload when it becomes visible.
        /// Reloads patients list and the ordonnances for the selected patient (if any).
        /// </summary>
        public void Refresh()
        {
            _ = LoadPatientsAsync();
            if (SelectedPatient != null)
            {
                _ = OnPatientSelectedAsync(SelectedPatient);
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
                        _notificationService.ShowSuccess("Ordonnance ajoutée avec succès.", "Succès"); // Notify success
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Erreur: {ex.Message}", "Erreur"); // Notify error
            }
        }

        private void AddTemplate()
        {
            try
            {
                var dialogViewModel = new OrdonnanceTemplateDialogViewModel(_templateService);
                var dialogView = new OrdonnanceTemplateDialogView { DataContext = dialogViewModel };

                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width / 2) - 400,
                    Top = SystemParameters.WorkArea.Top + 10,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                window.MouseLeftButtonDown += (s, e) => window.DragMove();

                dialogViewModel.CloseDialog = (result) =>
                {
                    if (result == true)
                    {
                        _notificationService.ShowSuccess("Modèle d'ordonnance ajouté avec succès.", "Succès"); // Notify success
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Erreur: {ex.Message}", "Erreur"); // Notify error
            }
        }

        private async void PrintOrdonnance(OrdonnanceDisplayRow ordonnance)
        {
            if (ordonnance?.OrdonnanceObj == null) return;

            try
            {
                // 1. Get current dynamic settings
                Point coords = await _templateService.GetStartingCoordinatesAsync();
                var meds = ordonnance.OrdonnanceObj.Medicaments
                                     .Select(m => $"{m.Nom} - {m.Posologie}").ToList();

                // 2. Generate the document with the background visible for the preview
                var documentForPreview = _templateService.CreatePrintDocument(meds, coords, isPreview: true);

                // 3. Show the window you saw in your image
                var previewWin = new PrintPreviewWindow(documentForPreview);
                previewWin.Owner = Application.Current.MainWindow;
                previewWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'aperçu: {ex.Message}");
            }
        }

        private void ViewOrdonnance(OrdonnanceDisplayRow ordonnance)
        {
            if (ordonnance == null || SelectedPatient == null)
                return;

            try
            {
                var dialogViewModel = new OrdonnanceDialogViewModel(
                    _ordonnanceService, 
                    SelectedPatient.Id, 
                    SelectedPatient.FullName, 
                    ordonnance.OrdonnanceObj);

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
        public Ordonnance OrdonnanceObj { get; set; }
        
        private int _medicamentsCount;
        public int MedicamentsCount 
        { 
            get => _medicamentsCount;
            set => SetProperty(ref _medicamentsCount, value);
        }
    }
}
