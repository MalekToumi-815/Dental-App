using Dental_App.Models;
using Dental_App.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;

namespace Dental_App.ViewModels
{
    public class AntecedentViewModel : BindableBase
    {
        private readonly IAntecedentService _antecedentService;
        private readonly IPatientService _patientService;
        private readonly ILiveSearchService<Patient> _liveSearchService;
        private Timer _searchDebounceTimer;
        private const int DEBOUNCE_DELAY_MS = 300;

        // --- Collections ---
        private ObservableCollection<Patient> _patients;
        public ObservableCollection<Patient> Patients
        {
            get { return _patients; }
            set { SetProperty(ref _patients, value); }
        }

        private ObservableCollection<Antecedant> _antecedents;
        public ObservableCollection<Antecedant> Antecedents
        {
            get { return _antecedents; }
            set { SetProperty(ref _antecedents, value); }
        }

        // --- Patient Sélectionné ---
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get { return _selectedPatient; }
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    if (value != null)
                    {
                        OnPatientSelectedAsync();
                    }
                }
            }
        }

        private string _selectedPatientName = string.Empty;
        public string SelectedPatientName
        {
            get { return _selectedPatientName; }
            set { SetProperty(ref _selectedPatientName, value); }
        }

        private string _patientDisplayName = string.Empty;
        public string PatientDisplayName
        {
            get { return _patientDisplayName; }
            set { SetProperty(ref _patientDisplayName, value); }
        }

        // --- Recherche Patient ---
        private string _patientSearchText = string.Empty;
        public string PatientSearchText
        {
            get { return _patientSearchText; }
            set
            {
                if (SetProperty(ref _patientSearchText, value))
                {
                    // Débouncer la recherche patient
                    _searchDebounceTimer?.Dispose();
                    _searchDebounceTimer = new Timer(
                        _ => Application.Current?.Dispatcher?.Invoke(() => _ = SearchPatientsAsync()),
                        null,
                        DEBOUNCE_DELAY_MS,
                        Timeout.Infinite
                    );
                }
            }
        }

        // --- Recherche Antécédents ---
        private string _searchText = string.Empty;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Débouncer la recherche antécédents
                    _searchDebounceTimer?.Dispose();
                    _searchDebounceTimer = new Timer(
                        _ => Application.Current?.Dispatcher?.Invoke(() => _ = FilterAntecedentsAsync()),
                        null,
                        DEBOUNCE_DELAY_MS,
                        Timeout.Infinite
                    );
                }
            }
        }

        // --- Modal ---
        private bool _isModalOpen;
        public bool IsModalOpen
        {
            get { return _isModalOpen; }
            set { SetProperty(ref _isModalOpen, value); }
        }

        private string _modalTitle = "Nouvel Antécédent";
        public string ModalTitle
        {
            get { return _modalTitle; }
            set { SetProperty(ref _modalTitle, value); }
        }

        // --- Champs formulaire ---
        private string _newNom = string.Empty;
        public string NewNom
        {
            get { return _newNom; }
            set { SetProperty(ref _newNom, value); }
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get { return _newDescription; }
            set { SetProperty(ref _newDescription, value); }
        }

        // --- État UI ---
        private int _antecedentsCount;
        public int AntecedentsCount
        {
            get { return _antecedentsCount; }
            set { SetProperty(ref _antecedentsCount, value); }
        }

        private bool _hasAntecedents;
        public bool HasAntecedents
        {
            get { return _hasAntecedents; }
            set { SetProperty(ref _hasAntecedents, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private bool _hasSelectedPatient;
        public bool HasSelectedPatient
        {
            get { return _hasSelectedPatient; }
            set { SetProperty(ref _hasSelectedPatient, value); }
        }

        private bool _hasPatients;
        public bool HasPatients
        {
            get { return _hasPatients; }
            set { SetProperty(ref _hasPatients, value); }
        }

        private Antecedant _selectedAntecedent;

        // --- Commands ---
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand<Antecedant> EditCommand { get; }
        public DelegateCommand<Antecedant> DeleteCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }
        public DelegateCommand ClearPatientSearchCommand { get; }
        public DelegateCommand<Patient> SelectPatientCommand { get; }

        public AntecedentViewModel(IAntecedentService antecedentService, IPatientService patientService, ILiveSearchService<Patient> liveSearchService)
        {
            _antecedentService = antecedentService ?? throw new ArgumentNullException(nameof(antecedentService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));

            Antecedents = new ObservableCollection<Antecedant>();
            Patients = new ObservableCollection<Patient>();
            
            // Initialize state - no patients shown by default
            HasPatients = false;
            HasSelectedPatient = false;

            OpenModalCommand = new DelegateCommand(OpenModal);
            CloseModalCommand = new DelegateCommand(CloseModal);
            SaveCommand = new DelegateCommand(SaveAntecedent);
            EditCommand = new DelegateCommand<Antecedant>(EditAntecedent);
            DeleteCommand = new DelegateCommand<Antecedant>(DeleteAntecedent);
            ClearSearchCommand = new DelegateCommand(ClearSearch);
            ClearPatientSearchCommand = new DelegateCommand(ClearPatientSearch);
            SelectPatientCommand = new DelegateCommand<Patient>(SelectPatient);

            Debug.WriteLine("[AntecedentViewModel] Initialisé");
        }

        /// <summary>
        /// Charge tous les patients par défaut depuis la base de données
        /// </summary>
        private async void LoadAllPatientsAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("[AntecedentViewModel] Chargement de tous les patients");

                var items = await _patientService.GetAllAsync();
                // Don't clear if we want to keep the list empty initially
                // Only populate when user searches
                
                Debug.WriteLine($"[AntecedentViewModel] {items.Count} patients disponibles");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadAllPatientsAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des patients: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Recherche les patients par nom et prnom
        /// </summary>
        private async Task SearchPatientsAsync()
        {
            try
            {
                Debug.WriteLine($"[SearchPatientsAsync] Recherche: '{PatientSearchText}'");

                // Only search if user has typed something
                if (string.IsNullOrWhiteSpace(PatientSearchText))
                {
                    // Hide the list when search is empty
                    Patients.Clear();
                    HasPatients = false;
                    return;
                }

                var results = await _liveSearchService.SearchAsync(PatientSearchText.Trim(), async (term) => 
                    await _patientService.SearchByNameAsync(term, 10));

                if (results != null)
                {
                    Patients.Clear();
                    foreach (var patient in results)
                    {
                        Patients.Add(patient);
                    }

                    HasPatients = Patients.Count > 0;
                    Debug.WriteLine($"[SearchPatientsAsync] {Patients.Count} rsultats trouvs");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SearchPatientsAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Réinitialise la recherche patient et cache la liste
        /// </summary>
        private void ClearPatientSearch()
        {
            Debug.WriteLine("[ClearPatientSearch] Réinitialisation de la recherche patient");
            PatientSearchText = string.Empty;
            Patients.Clear();
            HasPatients = false;
        }

        /// <summary>
        /// Sélectionne un patient et charge ses antécédents
        /// </summary>
        private void SelectPatient(Patient patient)
        {
            if (patient == null) return;

            Debug.WriteLine($"[SelectPatient] Patient slectionn: {patient.Nom}");
            SelectedPatient = patient;
            
            // Hide the patient search popup after selection
            HasPatients = false;
        }

        /// <summary>
        /// Appel quand un patient est slectionn
        /// </summary>
        private async void OnPatientSelectedAsync()
        {
            if (_selectedPatient == null) return;

            try
            {
                IsLoading = true;
                Debug.WriteLine($"[AntecedentViewModel] Patient sélectionné: {_selectedPatient.Nom}");

                // Mettre à jour l'affichage du patient
                SelectedPatientName = $"{_selectedPatient.Nom} {_selectedPatient.Prenom}";
                PatientDisplayName = $"Antécédents — {_selectedPatient.Nom} {_selectedPatient.Prenom}";
                HasSelectedPatient = true;

                // Charger les antécédents du patient
                await LoadAntecedentsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnPatientSelectedAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les antécédents du patient sélectionné
        /// </summary>
        private async Task LoadAntecedentsAsync()
        {
            try
            {
                if (_selectedPatient == null)
                {
                    Antecedents.Clear();
                    UpdateUI();
                    return;
                }

                Debug.WriteLine($"[LoadAntecedentsAsync] Chargement pour Patient: {_selectedPatient.Id}");

                var items = await _antecedentService.GetByPatientIdAsync(_selectedPatient.Id);
                Antecedents.Clear();

                foreach (var item in items)
                {
                    Antecedents.Add(item);
                }

                UpdateUI();
                Debug.WriteLine($"[LoadAntecedentsAsync] {Antecedents.Count} antécédents chargés");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadAntecedentsAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Filtre les antécédents en fonction du texte de recherche
        /// </summary>
        private async Task FilterAntecedentsAsync()
        {
            try
            {
                Debug.WriteLine($"[FilterAntecedentsAsync] Recherche: '{SearchText}'");

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadAntecedentsAsync();
                    return;
                }

                if (_selectedPatient == null)
                {
                    return;
                }

                var results = await _antecedentService.GetByPatientIdAsync(_selectedPatient.Id);
                var filtered = results
                    .Where(a => a.Nom.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                (a.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

                Antecedents.Clear();
                foreach (var item in filtered)
                {
                    Antecedents.Add(item);
                }

                UpdateUI();
                Debug.WriteLine($"[FilterAntecedentsAsync] {Antecedents.Count} résultats affichés");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FilterAntecedentsAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Réinitialise la recherche des antécédents
        /// </summary>
        private void ClearSearch()
        {
            Debug.WriteLine("[ClearSearch] Réinitialisation de la recherche antécédents");
            SearchText = string.Empty;
            _ = LoadAntecedentsAsync();
        }

        private void OpenModal()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Veuillez d'abord sélectionner un patient.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Debug.WriteLine("[OpenModal] Ouverture de la modal");
            ModalTitle = "Nouvel Antécédent";
            _selectedAntecedent = null;
            NewNom = string.Empty;
            NewDescription = string.Empty;
            IsModalOpen = true;
        }

        private void CloseModal()
        {
            Debug.WriteLine("[CloseModal] Fermeture de la modal");
            IsModalOpen = false;
        }

        private async void EditAntecedent(Antecedant antecedent)
        {
            if (antecedent == null) return;

            try
            {
                Debug.WriteLine($"[EditAntecedent] Édition de l'antécédent {antecedent.Id}");
                _selectedAntecedent = await _antecedentService.GetByIdAsync(antecedent.Id);

                if (_selectedAntecedent != null)
                {
                    ModalTitle = "Modifier l'Antécédent";
                    NewNom = _selectedAntecedent.Nom;
                    NewDescription = _selectedAntecedent.Description ?? string.Empty;
                    IsModalOpen = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditAntecedent] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveAntecedent()
        {
            if (string.IsNullOrWhiteSpace(NewNom))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedPatient == null)
            {
                MessageBox.Show("Aucun patient sélectionné.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_selectedAntecedent == null)
                {
                    Debug.WriteLine("[SaveAntecedent] Création d'un nouvel antécédent");
                    await _antecedentService.CreateAsync(new Antecedant
                    {
                        Nom = NewNom,
                        Description = NewDescription,
                        PatientId = _selectedPatient.Id
                    });
                }
                else
                {
                    Debug.WriteLine($"[SaveAntecedent] Mise à jour de l'antécédent {_selectedAntecedent.Id}");
                    _selectedAntecedent.Nom = NewNom;
                    _selectedAntecedent.Description = NewDescription;
                    await _antecedentService.UpdateAsync(_selectedAntecedent);
                }

                CloseModal();
                await LoadAntecedentsAsync();
                MessageBox.Show("Opération réussie.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveAntecedent] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteAntecedent(Antecedant antecedent)
        {
            if (antecedent == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'antécédent \"{antecedent.Nom}\"?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                Debug.WriteLine($"[DeleteAntecedent] Suppression de l'antécédent {antecedent.Id}");
                await _antecedentService.DeleteAsync(antecedent.Id);
                await LoadAntecedentsAsync();
                MessageBox.Show("Antécédent supprimé.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteAntecedent] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mises à jour l'UI (compteur, visibilité du message vide)
        /// </summary>
        private void UpdateUI()
        {
            AntecedentsCount = Antecedents.Count;
            HasAntecedents = Antecedents.Count > 0;
        }
    }
}
