using Prism.Mvvm;
using Prism.Commands;
using Dental_App.Services;
using Dental_App.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class RendezVousViewModel : BindableBase
    {
        private readonly IRendezVousService _rendezVousService;
        private readonly IPatientService _patientService;

        // --- Collections ---
        private ObservableCollection<RendezVousItemViewModel> _rendezVousList;
        public ObservableCollection<RendezVousItemViewModel> RendezVousList
        {
            get { return _rendezVousList; }
            set { SetProperty(ref _rendezVousList, value); }
        }

        // --- ViewModel pour la recherche prédictive de patients ---
        private PatientSearchViewModel _patientSearchViewModel;
        public PatientSearchViewModel PatientSearchViewModel
        {
            get => _patientSearchViewModel;
            private set => SetProperty(ref _patientSearchViewModel, value);
        }

        // --- Calendrier et affichage ---
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    Debug.WriteLine($"[SelectedDate] Nouvelle date: {value:dd/MM/yyyy}");
                    _ = LoadRendezVousByDateAsync();
                    _ = LoadStatisticsAsync();
                }
            }
        }

        private string _dateLabel;
        public string DateLabel
        {
            get { return _dateLabel; }
            set { SetProperty(ref _dateLabel, value); }
        }

        private int _todayCount;
        public int TodayCount
        {
            get { return _todayCount; }
            set { SetProperty(ref _todayCount, value); }
        }

        private int _weekCount;
        public int WeekCount
        {
            get { return _weekCount; }
            set { SetProperty(ref _weekCount, value); }
        }

        // --- États UI ---
        private bool _isModalVisible;
        public bool IsModalVisible
        {
            get { return _isModalVisible; }
            set { SetProperty(ref _isModalVisible, value); }
        }

        private string _modalTitle = "Nouveau Rendez-Vous";
        public string ModalTitle
        {
            get { return _modalTitle; }
            set { SetProperty(ref _modalTitle, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        // --- Champs Formulaire ---
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        private DateTime? _newRendezVousDate;
        public DateTime? NewRendezVousDate
        {
            get => _newRendezVousDate;
            set => SetProperty(ref _newRendezVousDate, value);
        }

        private string _selectedTimeSlot;
        public string SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set => SetProperty(ref _selectedTimeSlot, value);
        }

        private string _newMotif = string.Empty;
        public string NewMotif
        {
            get => _newMotif;
            set => SetProperty(ref _newMotif, value); }

        private bool _smsReminder = true;
        public bool SmsReminder
        {
            get => _smsReminder;
            set => SetProperty(ref _smsReminder, value);
        }

        private RendezVou _selectedRendezVous;

        // --- Commands ---
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveRendezVousCommand { get; }
        public DelegateCommand<RendezVousItemViewModel> EditRendezVousCommand { get; }

        public RendezVousViewModel(IRendezVousService rendezVousService, IPatientService patientService)
        {
            _rendezVousService = rendezVousService ?? throw new ArgumentNullException(nameof(rendezVousService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            RendezVousList = new ObservableCollection<RendezVousItemViewModel>();

            // Initialiser le ViewModel de recherche patient
            PatientSearchViewModel = new PatientSearchViewModel(_patientService);

            OpenModalCommand = new DelegateCommand(OpenModal);
            CloseModalCommand = new DelegateCommand(CloseModal);
            SaveRendezVousCommand = new DelegateCommand(SaveRendezVous);
            EditRendezVousCommand = new DelegateCommand<RendezVousItemViewModel>(EditRendezVous);

            // S'abonner à l'événement de sélection du patient
            PatientSearchViewModel.OnPatientSelected += OnPatientSelectedHandler;

            Debug.WriteLine("[ViewModel] RendezVousViewModel initialisé");
            LoadInitialData();
        }

        private async void LoadInitialData()
        {
            Debug.WriteLine("[LoadInitialData] Chargement initial");
            await LoadRendezVousByDateAsync();
            await LoadStatisticsAsync();
            UpdateDateLabel();
        }

        /// <summary>
        /// Charge les rendez-vous pour la date sélectionnée
        /// </summary>
        private async Task LoadRendezVousByDateAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine($"[LoadRendezVousByDateAsync] Chargement pour {SelectedDate:dd/MM/yyyy}");

                // Récupérer les rendez-vous de la date sélectionnée
                var startDate = SelectedDate.Date;
                var endDate = SelectedDate.Date.AddDays(1);

                var rendezVous = await _rendezVousService.GetByDateRangeAsync(startDate, endDate);
                Debug.WriteLine($"[LoadRendezVousByDateAsync] {rendezVous.Count} rendez-vous trouvés");

                RendezVousList.Clear();
                foreach (var rv in rendezVous)
                {
                    var initiales = GetPatientInitials(rv.Patient);
                    RendezVousList.Add(new RendezVousItemViewModel
                    {
                        Id = rv.Id,
                        Heure = rv.DateDebut.ToString("HH:mm"),
                        NomPatient = $"{rv.Patient.Nom} {rv.Patient.Prenom}",
                        InitialesPatient = initiales,
                        Motif = "", // À récupérer des données associées si disponible
                        Statut = rv.Statut ?? "En attente"
                    });
                }

                UpdateDateLabel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadRendezVousByDateAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les statistiques (pour la date sélectionnée et cette semaine)
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                // Rendez-vous pour la date sélectionnée (au lieu de DateTime.Today)
                var selectedDateStart = SelectedDate.Date;
                var selectedDateEnd = selectedDateStart.AddDays(1);
                var selectedDateRvs = await _rendezVousService.GetByDateRangeAsync(selectedDateStart, selectedDateEnd);
                TodayCount = selectedDateRvs.Count;

                // Rendez-vous de la semaine contenant la date sélectionnée
                var weekStart = SelectedDate.AddDays(-(int)SelectedDate.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);
                var weekRvs = await _rendezVousService.GetByDateRangeAsync(weekStart, weekEnd);
                WeekCount = weekRvs.Count;

                Debug.WriteLine($"[LoadStatisticsAsync] Date sélectionnée ({SelectedDate:dd/MM/yyyy}): {TodayCount}, Cette semaine: {WeekCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadStatisticsAsync] ERREUR: {ex.Message}");
            }
        }

        private void UpdateDateLabel()
        {
            var culture = new System.Globalization.CultureInfo("fr-FR");
            DateLabel = SelectedDate.ToString("dd MMMM yyyy", culture);
        }

        private string GetPatientInitials(Patient patient)
        {
            if (patient == null) return "??";
            var nom = patient.Nom?.FirstOrDefault().ToString() ?? "";
            var prenom = patient.Prenom?.FirstOrDefault().ToString() ?? "";
            return $"{nom}{prenom}".ToUpper();
        }

        /// <summary>
        /// Appelé automatiquement lors de la sélection d'un patient via la recherche.
        /// </summary>
        private void OnPatientSelectedHandler(Patient patient)
        {
            Debug.WriteLine($"[OnPatientSelectedHandler] Patient sélectionné: {patient?.Nom} {patient?.Prenom}");
            SelectedPatient = patient;
        }

        private void OpenModal()
        {
            Debug.WriteLine("[OpenModal] Ouverture de la modal");
            ModalTitle = "Nouveau Rendez-Vous";
            _selectedRendezVous = null;
            SelectedPatient = null;
            NewRendezVousDate = SelectedDate;
            SelectedTimeSlot = null;
            NewMotif = string.Empty;
            SmsReminder = true;

            // Réinitialiser la recherche de patient
            PatientSearchViewModel.Reset();

            IsModalVisible = true;
        }

        private void CloseModal()
        {
            Debug.WriteLine("[CloseModal] Fermeture de la modal");
            IsModalVisible = false;
            PatientSearchViewModel.Reset();
        }

        private async void EditRendezVous(RendezVousItemViewModel item)
        {
            if (item == null) return;

            try
            {
                Debug.WriteLine($"[EditRendezVous] Modification du rendez-vous {item.Id}");
                _selectedRendezVous = await _rendezVousService.GetByIdAsync(item.Id);
                
                if (_selectedRendezVous != null)
                {
                    ModalTitle = "Modifier le Rendez-Vous";

                    SelectedPatient = _selectedRendezVous.Patient;
                    NewRendezVousDate = _selectedRendezVous.DateDebut.Date;
                    SelectedTimeSlot = _selectedRendezVous.DateDebut.ToString("HH:mm");

                    // Mettre à jour la recherche avec le patient courant
                    PatientSearchViewModel.SearchText = $"{_selectedRendezVous.Patient.Nom} {_selectedRendezVous.Patient.Prenom}";

                    IsModalVisible = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditRendezVous] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveRendezVous()
        {
            if (SelectedPatient == null || !NewRendezVousDate.HasValue || string.IsNullOrWhiteSpace(SelectedTimeSlot))
            {
                MessageBox.Show("Veuillez remplir les champs obligatoires (Patient, Date, Heure).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Parser l'heure
                if (!TimeSpan.TryParse(SelectedTimeSlot, out var timeSpan))
                {
                    MessageBox.Show("Format d'heure invalide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dateDebut = NewRendezVousDate.Value.Date.Add(timeSpan);

                // Vérifier les conflits
                var hasConflict = await _rendezVousService.HasConflictAsync(
                    SelectedPatient.Id,
                    dateDebut,
                    _selectedRendezVous?.Id
                );

                if (hasConflict)
                {
                    MessageBox.Show("Ce créneau est déjà occupé pour ce patient.", "Conflit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedRendezVous == null)
                {
                    // Créer un nouveau rendez-vous
                    Debug.WriteLine("[SaveRendezVous] Création d'un nouveau rendez-vous");
                    await _rendezVousService.CreateAsync(new RendezVou
                    {
                        PatientId = SelectedPatient.Id,
                        DateDebut = dateDebut,
                        Statut = "en attente"
                    });
                    MessageBox.Show("Rendez-vous créé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Modifier le rendez-vous existant
                    Debug.WriteLine($"[SaveRendezVous] Mise à jour du rendez-vous {_selectedRendezVous.Id}");
                    _selectedRendezVous.PatientId = SelectedPatient.Id;
                    _selectedRendezVous.DateDebut = dateDebut;
                    await _rendezVousService.UpdateAsync(_selectedRendezVous);
                    MessageBox.Show("Rendez-vous mis à jour avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseModal();
                await LoadRendezVousByDateAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveRendezVous] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RendezVousItemViewModel : BindableBase
    {
        private int _id;
        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _heure;
        public string Heure
        {
            get { return _heure; }
            set { SetProperty(ref _heure, value); }
        }

        private string _nomPatient;
        public string NomPatient
        {
            get { return _nomPatient; }
            set { SetProperty(ref _nomPatient, value); }
        }

        private string _initialesPatient;
        public string InitialesPatient
        {
            get { return _initialesPatient; }
            set { SetProperty(ref _initialesPatient, value); }
        }

        private string _motif;
        public string Motif
        {
            get { return _motif; }
            set { SetProperty(ref _motif, value); }
        }

        private string _statut;
        public string Statut
        {
            get { return _statut; }
            set { SetProperty(ref _statut, value); }
        }
    }
}
