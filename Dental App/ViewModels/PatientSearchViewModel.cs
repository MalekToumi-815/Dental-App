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
    /// <summary>
    /// ViewModel ddie  la recherche prdictive de patients.
    /// Gre la logique de filtrage et de slection avec une sparation des responsabilits.
    /// </summary>
    public class PatientSearchViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly ILiveSearchService<Patient> _liveSearchService;
        private Timer _searchDebounceTimer;
        private const int DEBOUNCE_DELAY_MS = 300;

        // --- Collections ---
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

        // Backwards compatibility constructor
        public PatientSearchViewModel(IPatientService patientService)
            : this(patientService, null)
        {
        }

        public PatientSearchViewModel(IPatientService patientService, ILiveSearchService<Patient> liveSearchService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _liveSearchService = liveSearchService;

            SuggestedPatients = new ObservableCollection<Patient>();

            ClearSearchCommand = new DelegateCommand(ClearSearch);
        }

        /// <summary>
        /// Effectue une recherche prdictive sur le texte saisi.
        /// La recherche est insensible  la casse et utilise l'algorithme "Contains" sur le nom et prnom.
        /// </summary>
        private async Task PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                SuggestedPatients.Clear();
                IsSuggestionsVisible = false;
                return;
            }

            if (_liveSearchService != null)
            {
                try
                {
                    var results = await _liveSearchService.SearchAsync(searchText, async (term) => 
                        await _patientService.SearchByNameAsync(term));

                    if (results != null)
                    {
                        var filtered = results.OrderBy(p => p.Nom).ThenBy(p => p.Prenom).ToList();
                        SuggestedPatients.Clear();
                        foreach (var p in filtered)
                        {
                            SuggestedPatients.Add(p);
                        }
                        IsSuggestionsVisible = SuggestedPatients.Any();
                        Debug.WriteLine($"[PatientSearchViewModel] {SuggestedPatients.Count} rsultats pour '{searchText}'");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PatientSearchViewModel] ERREUR de recherche live: {ex.Message}");
                }
            }
            else 
            {
                // Fallback mechanism if _liveSearchService is not provided
                try
                {
                   var patients = await _patientService.GetAllAsync();
                   var filtered = patients
                        .Where(p => 
                            (p.Nom != null && p.Nom.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (p.Prenom != null && p.Prenom.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            ($"{p.Nom} {p.Prenom}".Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        )
                        .OrderBy(p => p.Nom)
                        .ThenBy(p => p.Prenom)
                        .Take(10)
                        .ToList();

                    SuggestedPatients.Clear();
                    foreach (var p in filtered)
                    {
                        SuggestedPatients.Add(p);
                    }
                    IsSuggestionsVisible = SuggestedPatients.Any();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PatientSearchViewModel] ERREUR de recherche: {ex.Message}");
                }
            }
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
