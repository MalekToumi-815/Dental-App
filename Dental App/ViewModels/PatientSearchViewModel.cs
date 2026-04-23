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

namespace Dental_App.ViewModels
{
    /// <summary>
    /// ViewModel dédiée à la recherche prédictive de patients.
    /// Gère la logique de filtrage et de sélection avec une séparation des responsabilités.
    /// </summary>
    public class PatientSearchViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private Timer _searchDebounceTimer;
        private const int DEBOUNCE_DELAY_MS = 300;

        // --- Collections ---
        private ObservableCollection<Patient> _allPatients;
        private ObservableCollection<Patient> _suggestedPatients;

        public ObservableCollection<Patient> SuggestedPatients
        {
            get => _suggestedPatients;
            set => SetProperty(ref _suggestedPatients, value);
        }

        // --- États UI ---
        private bool _isSuggestionsVisible;
        public bool IsSuggestionsVisible
        {
            get => _isSuggestionsVisible;
            set => SetProperty(ref _isSuggestionsVisible, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Débouncer la recherche pour éviter trop de filtrage
                    _searchDebounceTimer?.Dispose();
                    _searchDebounceTimer = new Timer(
                        _ => Application.Current?.Dispatcher?.Invoke(() => PerformSearch(value)),
                        null,
                        DEBOUNCE_DELAY_MS,
                        Timeout.Infinite
                    );
                }
            }
        }

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value) && value != null)
                {
                    // Mettre à jour le texte de recherche avec le patient sélectionné
                    SearchText = $"{value.Nom} {value.Prenom}";
                    IsSuggestionsVisible = false;
                    
                    // Déclencher l'événement de sélection
                    OnPatientSelected?.Invoke(value);
                }
            }
        }

        // --- Events pour la communication inter-ViewModels ---
        public event Action<Patient> OnPatientSelected;

        // --- Commands ---
        public DelegateCommand ClearSearchCommand { get; }

        public PatientSearchViewModel(IPatientService patientService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            _allPatients = new ObservableCollection<Patient>();
            SuggestedPatients = new ObservableCollection<Patient>();

            ClearSearchCommand = new DelegateCommand(ClearSearch);

            LoadPatients();
        }

        /// <summary>
        /// Charge tous les patients depuis la base de données.
        /// </summary>
        private async void LoadPatients()
        {
            try
            {
                var patients = await _patientService.GetAllAsync();
                _allPatients.Clear();
                foreach (var p in patients)
                {
                    _allPatients.Add(p);
                }
                Debug.WriteLine($"[PatientSearchViewModel] {_allPatients.Count} patients chargés");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PatientSearchViewModel] ERREUR lors du chargement: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des patients: {ex.Message}");
            }
        }

        /// <summary>
        /// Effectue une recherche prédictive sur le texte saisi.
        /// La recherche est insensible à la casse et utilise l'algorithme "Contains" sur le nom et prénom.
        /// </summary>
        private void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                SuggestedPatients.Clear();
                IsSuggestionsVisible = false;
                return;
            }

            // Recherche insensible à la casse sur le nom et prénom
            var filtered = _allPatients
                .Where(p => 
                    (p.Nom != null && p.Nom.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Prenom != null && p.Prenom.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    ($"{p.Nom} {p.Prenom}".Contains(searchText, StringComparison.OrdinalIgnoreCase))
                )
                .OrderBy(p => p.Nom)
                .ThenBy(p => p.Prenom)
                .ToList();

            SuggestedPatients.Clear();
            foreach (var p in filtered)
            {
                SuggestedPatients.Add(p);
            }

            // Afficher les suggestions uniquement s'il y a des résultats
            IsSuggestionsVisible = SuggestedPatients.Any();
            Debug.WriteLine($"[PatientSearchViewModel] {SuggestedPatients.Count} résultats pour '{searchText}'");
        }

        /// <summary>
        /// Réinitialise la recherche.
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedPatient = null;
            SuggestedPatients.Clear();
            IsSuggestionsVisible = false;
        }

        /// <summary>
        /// Réinitialise le ViewModel à son état initial.
        /// Utile lors de la fermeture d'un modal.
        /// </summary>
        public void Reset()
        {
            ClearSearch();
        }
    }
}
