using Dental_App.Models;
using Dental_App.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Dental_App.ViewModels
{
    /// <summary>
    /// ViewModel dédié à la recherche prédictive de prothésistes.
    /// Gère la logique de filtrage et de sélection avec une séparation des responsabilités.
    /// </summary>
    public class ProthesisteSearchViewModel : BindableBase
    {
        private readonly IProthesisteService _prothesisteService;
        private Timer _searchDebounceTimer;
        private const int DEBOUNCE_DELAY_MS = 300;

        // --- Collections ---
        private ObservableCollection<Prothesiste> _allProthesistes;
        private ObservableCollection<Prothesiste> _suggestedProthesistes;

        public ObservableCollection<Prothesiste> SuggestedProthesistes
        {
            get => _suggestedProthesistes;
            set => SetProperty(ref _suggestedProthesistes, value);
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

        private Prothesiste _selectedProthesiste;
        public Prothesiste SelectedProthesiste
        {
            get => _selectedProthesiste;
            set
            {
                if (SetProperty(ref _selectedProthesiste, value) && value != null)
                {
                    // Mettre à jour le texte de recherche avec le prothésiste sélectionné
                    SearchText = value.Nom;
                    IsSuggestionsVisible = false;
                    
                    // Déclencher l'événement de sélection
                    OnProthesisteSelected?.Invoke(value);
                }
            }
        }

        // --- Events pour la communication inter-ViewModels ---
        public event Action<Prothesiste> OnProthesisteSelected;

        // --- Commands ---
        public DelegateCommand ClearSearchCommand { get; }

        public ProthesisteSearchViewModel(IProthesisteService prothesisteService)
        {
            _prothesisteService = prothesisteService ?? throw new ArgumentNullException(nameof(prothesisteService));

            _allProthesistes = new ObservableCollection<Prothesiste>();
            SuggestedProthesistes = new ObservableCollection<Prothesiste>();

            ClearSearchCommand = new DelegateCommand(ClearSearch);

            LoadProthesistes();
        }

        /// <summary>
        /// Charge tous les prothésistes depuis la base de données.
        /// </summary>
        private async void LoadProthesistes()
        {
            try
            {
                var prothesistes = await _prothesisteService.GetAllAsync();
                _allProthesistes.Clear();
                foreach (var p in prothesistes)
                {
                    _allProthesistes.Add(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des prothésistes: {ex.Message}");
            }
        }

        /// <summary>
        /// Effectue une recherche prédictive sur le texte saisi.
        /// La recherche est insensible à la casse et utilise l'algorithme "Contains".
        /// </summary>
        private void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                SuggestedProthesistes.Clear();
                IsSuggestionsVisible = false;
                return;
            }

            // Recherche insensible à la casse
            var filtered = _allProthesistes
                .Where(p => p.Nom != null && p.Nom.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Nom) // Tri alphabétique des résultats
                .ToList();

            SuggestedProthesistes.Clear();
            foreach (var p in filtered)
            {
                SuggestedProthesistes.Add(p);
            }

            // Afficher les suggestions uniquement s'il y a des résultats
            IsSuggestionsVisible = SuggestedProthesistes.Any();
        }

        /// <summary>
        /// Réinitialise la recherche.
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedProthesiste = null;
            SuggestedProthesistes.Clear();
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
