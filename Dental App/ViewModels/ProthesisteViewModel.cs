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
    public class ProthesisteViewModel : BindableBase
    {
        private readonly IProthesisteService _prothesisteService;
        private Timer _searchDebounceTimer;
        private const int DEBOUNCE_DELAY_MS = 300;

        // Liste des prothésistes
        private ObservableCollection<ProthesisteDisplayItem> _prosthesists;
        public ObservableCollection<ProthesisteDisplayItem> Prosthesists
        {
            get { return _prosthesists; }
            set { SetProperty(ref _prosthesists, value); }
        }

        // Texte de recherche avec logique hybride (nom ou téléphone)
        private string _searchText = string.Empty;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Débouncer la recherche pour éviter trop d'appels réseau
                    _searchDebounceTimer?.Dispose();
                    _searchDebounceTimer = new Timer(
                        _ => Application.Current?.Dispatcher?.Invoke(() => _ = FilterProthesistesAsync()),
                        null,
                        DEBOUNCE_DELAY_MS,
                        Timeout.Infinite
                    );
                }
            }
        }

        // Modal
        private bool _isModalOpen;
        public bool IsModalOpen
        {
            get { return _isModalOpen; }
            set { SetProperty(ref _isModalOpen, value); }
        }

        // Champs formulaire
        private string _newNom = string.Empty;
        public string NewNom
        {
            get { return _newNom; }
            set { SetProperty(ref _newNom, value); }
        }

        private string _newAdresse = string.Empty;
        public string NewAdresse
        {
            get { return _newAdresse; }
            set { SetProperty(ref _newAdresse, value); }
        }

        private string _newTelephone = string.Empty;
        public string NewTelephone
        {
            get { return _newTelephone; }
            set { SetProperty(ref _newTelephone, value); }
        }

        private Prothesiste _selectedProthesiste;

        // Commands
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand<ProthesisteDisplayItem> EditCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }

        public ProthesisteViewModel(IProthesisteService prothesisteService)
        {
            _prothesisteService = prothesisteService ?? throw new ArgumentNullException(nameof(prothesisteService));

            Prosthesists = new ObservableCollection<ProthesisteDisplayItem>();

            // Initialize commands
            OpenModalCommand = new DelegateCommand(OpenModal);
            CloseModalCommand = new DelegateCommand(CloseModal);
            SaveCommand = new DelegateCommand(SaveProthesiste);
            EditCommand = new DelegateCommand<ProthesisteDisplayItem>(EditProthesiste);
            ClearSearchCommand = new DelegateCommand(ClearSearch);

            // Load data
            LoadProthesistes();
        }

        /// <summary>
        /// Load all prothésistes from database with their command count
        /// </summary>
        private async void LoadProthesistes()
        {
            try
            {
                var prothesistes = await _prothesisteService.GetAllAsync();
                
                Prosthesists.Clear();
                foreach (var p in prothesistes)
                {
                    Prosthesists.Add(new ProthesisteDisplayItem
                    {
                        Id = p.Id,
                        Nom = p.Nom,
                        Adresse = p.Adresse ?? string.Empty,
                        Telephone = p.Tel ?? string.Empty,
                        NbCommandes = p.CommandeProthesistes.Count
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des prothésistes: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Filtrer les prothésistes en fonction du texte de recherche.
        /// Détecte automatiquement si la recherche est par nom ou par téléphone.
        /// </summary>
        private async Task FilterProthesistesAsync()
        {
            try
            {
                // Si la recherche est vide, afficher tous les prothésistes
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    LoadProthesistes();
                    return;
                }

                List<Prothesiste> results;

                // Si la saisie contient uniquement des chiffres -> Recherche par téléphone
                if (SearchText.All(char.IsDigit) || SearchText.All(c => char.IsDigit(c) || c == ' ' || c == '-' || c == '+'))
                {
                    results = await _prothesisteService.GetByPhoneAsync(SearchText);
                }
                // Sinon -> Recherche par nom
                else
                {
                    results = await _prothesisteService.GetByNameAsync(SearchText);
                }

                // Mettre à jour la liste affichée
                Prosthesists.Clear();
                foreach (var p in results)
                {
                    Prosthesists.Add(new ProthesisteDisplayItem
                    {
                        Id = p.Id,
                        Nom = p.Nom,
                        Adresse = p.Adresse ?? string.Empty,
                        Telephone = p.Tel ?? string.Empty,
                        NbCommandes = p.CommandeProthesistes.Count
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clear the search and reload all prothésistes
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
            LoadProthesistes();
        }

        /// <summary>
        /// Open the modal for creating a new prothésiste
        /// </summary>
        private void OpenModal()
        {
            _selectedProthesiste = null;
            NewNom = string.Empty;
            NewAdresse = string.Empty;
            NewTelephone = string.Empty;
            IsModalOpen = true;
        }

        /// <summary>
        /// Close the modal
        /// </summary>
        private void CloseModal()
        {
            IsModalOpen = false;
            NewNom = string.Empty;
            NewAdresse = string.Empty;
            NewTelephone = string.Empty;
            _selectedProthesiste = null;
        }

        /// <summary>
        /// Save a new prothésiste or update an existing one
        /// </summary>
        private async void SaveProthesiste()
        {
            if (string.IsNullOrWhiteSpace(NewNom))
            {
                MessageBox.Show("Le nom du prothésiste est requis.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_selectedProthesiste == null)
                {
                    // Create new
                    var newProthesiste = new Prothesiste
                    {
                        Nom = NewNom.Trim(),
                        Adresse = string.IsNullOrWhiteSpace(NewAdresse) ? null : NewAdresse.Trim(),
                        Tel = string.IsNullOrWhiteSpace(NewTelephone) ? null : NewTelephone.Trim()
                    };

                    await _prothesisteService.CreateAsync(newProthesiste);
                    MessageBox.Show("Prothésiste créé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Update existing
                    _selectedProthesiste.Nom = NewNom.Trim();
                    _selectedProthesiste.Adresse = string.IsNullOrWhiteSpace(NewAdresse) ? null : NewAdresse.Trim();
                    _selectedProthesiste.Tel = string.IsNullOrWhiteSpace(NewTelephone) ? null : NewTelephone.Trim();

                    await _prothesisteService.UpdateAsync(_selectedProthesiste);
                    MessageBox.Show("Prothésiste mis à jour avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseModal();
                LoadProthesistes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edit an existing prothésiste
        /// </summary>
        private async void EditProthesiste(ProthesisteDisplayItem item)
        {
            if (item == null) return;

            try
            {
                _selectedProthesiste = await _prothesisteService.GetByIdAsync(item.Id);
                
                if (_selectedProthesiste != null)
                {
                    NewNom = _selectedProthesiste.Nom;
                    NewAdresse = _selectedProthesiste.Adresse ?? string.Empty;
                    NewTelephone = _selectedProthesiste.Tel ?? string.Empty;
                    IsModalOpen = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Display item for Prothesiste in the view
    /// </summary>
    public class ProthesisteDisplayItem
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Adresse { get; set; }
        public string Telephone { get; set; }
        public int NbCommandes { get; set; }
    }
}

