using Dental_App.Models;
using Dental_App.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class CommandeProthesisteViewModel : BindableBase
    {
        private readonly ICommandeProthesisteService _commandeService;
        private readonly IProthesisteService _prothesisteService;
        private readonly ILiveSearchService<CommandeProthesiste> _liveSearchService;

        // --- Collections ---
        private ObservableCollection<CommandeProthesisteDisplayItem> _commandes;
        public ObservableCollection<CommandeProthesisteDisplayItem> Commandes
        {
            get => _commandes;
            set => SetProperty(ref _commandes, value);
        }

        // --- ViewModel pour la recherche prédictive ---
        private ProthesisteSearchViewModel _prothesisteSearchViewModel;
        public ProthesisteSearchViewModel ProthesisteSearchViewModel
        {
            get => _prothesisteSearchViewModel;
            private set => SetProperty(ref _prothesisteSearchViewModel, value);
        }

        // --- Texte de recherche pour filtrer les commandes ---
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    Debug.WriteLine($"[SearchText] Nouvelle valeur: '{value}'");
                    
                    _ = FilterCommandesAsync();
                }
            }
        }

        // --- État de chargement ---
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // --- États UI ---
        private bool _isModalOpen;
        public bool IsModalOpen
        {
            get => _isModalOpen;
            set => SetProperty(ref _isModalOpen, value);
        }

        private string _modalTitle = "Nouvelle Commande";
        public string ModalTitle
        {
            get => _modalTitle;
            set => SetProperty(ref _modalTitle, value);
        }

        // --- Champs Formulaire ---
        private Prothesiste _selectedProthesiste;
        public Prothesiste SelectedProthesiste
        {
            get => _selectedProthesiste;
            set => SetProperty(ref _selectedProthesiste, value);
        }

        private DateTime? _newDate;
        public DateTime? NewDate
        {
            get => _newDate;
            set => SetProperty(ref _newDate, value);
        }

        private string _newAchats = string.Empty;
        public string NewAchats
        {
            get => _newAchats;
            set => SetProperty(ref _newAchats, value);
        }

        private string _newSommePayee = "0";
        public string NewSommePayee
        {
            get => _newSommePayee;
            set => SetProperty(ref _newSommePayee, value);
        }

        private CommandeProthesiste _selectedCommande;

        // --- Commands ---
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand<CommandeProthesisteDisplayItem> EditCommand { get; }
        public DelegateCommand PrepareCommandeCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }

        public CommandeProthesisteViewModel(ICommandeProthesisteService commandeService, IProthesisteService prothesisteService, ILiveSearchService<CommandeProthesiste> liveSearchService)
        {
            _commandeService = commandeService ?? throw new ArgumentNullException(nameof(commandeService));
            _prothesisteService = prothesisteService ?? throw new ArgumentNullException(nameof(prothesisteService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));

            // Initialiser le ViewModel de recherche
            ProthesisteSearchViewModel = new ProthesisteSearchViewModel(_prothesisteService);

            Commandes = new ObservableCollection<CommandeProthesisteDisplayItem>();

            OpenModalCommand = new DelegateCommand(OpenModal);
            CloseModalCommand = new DelegateCommand(CloseModal);
            SaveCommand = new DelegateCommand(SaveCommande);
            EditCommand = new DelegateCommand<CommandeProthesisteDisplayItem>(EditCommande);
            PrepareCommandeCommand = new DelegateCommand(PrepareCommande);
            ClearSearchCommand = new DelegateCommand(ClearSearch);

            // S'abonner à l'événement de sélection du prothésiste
            ProthesisteSearchViewModel.OnProthesisteSelected += OnProthesisteSelectedHandler;

            Debug.WriteLine("[ViewModel] CommandeProthesisteViewModel initialisé");
            LoadData();
        }

        private async void LoadData()
        {
            Debug.WriteLine("[LoadData] Chargement initial des commandes");
            await LoadCommandes();
        }


        private async Task LoadCommandes()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("[LoadCommandes] Début du chargement");
                
                var items = await _commandeService.GetAllAsync();
                Debug.WriteLine($"[LoadCommandes] {items.Count} commandes chargées");
                
                Commandes.Clear();
                foreach (var c in items)
                {
                    Commandes.Add(new CommandeProthesisteDisplayItem
                    {
                        Id = c.Id,
                        ProthesisteNom = c.IdProthesisteNavigation?.Nom ?? "N/A",
                        Date = c.Date ?? DateTime.Now,
                        Achats = c.Achats ?? string.Empty,
                        SommePayee = (decimal)(c.SommePayees ?? 0)
                    });
                }
                
                Debug.WriteLine($"[LoadCommandes] {Commandes.Count} commandes affichées");
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"[LoadCommandes] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur commandes: {ex.Message}"); 
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filtre les commandes en fonction du texte de recherche (nom ou téléphone du prothésiste).
        /// </summary>
        private async Task FilterCommandesAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine($"[FilterCommandesAsync] Début avec SearchText='{SearchText}'");

                // Si la recherche est vide, afficher toutes les commandes
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    Debug.WriteLine("[FilterCommandesAsync] Recherche vide, chargement complet");
                    await LoadCommandes();
                    return;
                }

                var results = await _liveSearchService.SearchAsync(SearchText, async (searchTerm) => 
                {
                    // Si la saisie contient uniquement des chiffres -> Recherche par téléphone
                    if (searchTerm.All(char.IsDigit) || searchTerm.All(c => char.IsDigit(c) || c == ' ' || c == '-' || c == '+'))
                    {
                        Debug.WriteLine($"[FilterCommandesAsync] Recherche par TELEPHONE: {searchTerm}");
                        return await _commandeService.GetByProthesistePhoneAsync(searchTerm);
                    }
                    // Sinon -> Recherche par nom
                    else
                    {
                        Debug.WriteLine($"[FilterCommandesAsync] Recherche par NOM: {searchTerm}");
                        return await _commandeService.GetByProthesisteNameAsync(searchTerm);
                    }
                });

                if (results == null) return; // Search was cancelled

                Debug.WriteLine($"[FilterCommandesAsync] {results.Count()} résultats trouvés");

                // Mettre à jour la liste affichée
                Commandes.Clear();
                foreach (var c in results)
                {
                    Commandes.Add(new CommandeProthesisteDisplayItem
                    {
                        Id = c.Id,
                        ProthesisteNom = c.IdProthesisteNavigation?.Nom ?? "N/A",
                        Date = c.Date ?? DateTime.Now,
                        Achats = c.Achats ?? string.Empty,
                        SommePayee = (decimal)(c.SommePayees ?? 0)
                    });
                }
                
                Debug.WriteLine($"[FilterCommandesAsync] {Commandes.Count} commandes affichées");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FilterCommandesAsync] ERREUR: {ex}");
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Réinitialise la recherche et affiche toutes les commandes
        /// </summary>
        private void ClearSearch()
        {
            Debug.WriteLine("[ClearSearch] Réinitialisation de la recherche");
            SearchText = string.Empty;
            _ = LoadCommandes();
        }

        /// <summary>
        /// Appelé automatiquement lors de la sélection d'un prothésiste via la recherche.
        /// Prépare l'interface pour créer une commande avec ce prothésiste.
        /// </summary>
        private void OnProthesisteSelectedHandler(Prothesiste prothesiste)
        {
            Debug.WriteLine($"[OnProthesisteSelectedHandler] Prothésiste sélectionné: {prothesiste?.Nom}");
            SelectedProthesiste = prothesiste;
        }

        /// <summary>
        /// Prépare la création d'une commande pour le prothésiste sélectionné.
        /// Cette méthode est appelée après la sélection d'un prothésiste.
        /// </summary>
        private void PrepareCommande()
        {
            if (SelectedProthesiste == null)
            {
                MessageBox.Show("Veuillez sélectionner un prothésiste.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Les champs sont déjà pré-remplis avec le prothésiste sélectionné
            // L'utilisateur peut modifier les autres champs et cliquer sur "Enregistrer"
        }

        private void OpenModal()
        {
            Debug.WriteLine("[OpenModal] Ouverture de la modal");
            ModalTitle = "Nouvelle Commande";
            _selectedCommande = null;
            SelectedProthesiste = null;
            NewDate = DateTime.Today;
            NewAchats = string.Empty;
            NewSommePayee = "0";

            // Réinitialiser la recherche de prothésiste
            ProthesisteSearchViewModel.Reset();

            IsModalOpen = true;
        }

        private async void EditCommande(CommandeProthesisteDisplayItem item)
        {
            if (item == null) return;

            try
            {
                Debug.WriteLine($"[EditCommande] Modification de la commande {item.Id}");
                _selectedCommande = await _commandeService.GetByIdAsync(item.Id);
                if (_selectedCommande != null)
                {
                    ModalTitle = "Modifier la Commande";

                    var pro = await _prothesisteService.GetByIdAsync(_selectedCommande.IdProthesiste);
                    SelectedProthesiste = pro;

                    // Mettre à jour la recherche avec le prothésiste courant
                    ProthesisteSearchViewModel.SearchText = pro?.Nom ?? string.Empty;

                    NewDate = _selectedCommande.Date;
                    NewAchats = _selectedCommande.Achats;
                    NewSommePayee = _selectedCommande.SommePayees.ToString();

                    IsModalOpen = true;
                }
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"[EditCommande] ERREUR: {ex.Message}");
                MessageBox.Show(ex.Message); 
            }
        }

        private async void SaveCommande()
        {
            if (SelectedProthesiste == null || !NewDate.HasValue || string.IsNullOrWhiteSpace(NewAchats))
            {
                MessageBox.Show("Veuillez remplir les champs obligatoires.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(NewSommePayee, out decimal somme)) somme = 0;

            try
            {
                if (_selectedCommande == null)
                {
                    Debug.WriteLine("[SaveCommande] Création d'une nouvelle commande");
                    await _commandeService.CreateAsync(new CommandeProthesiste
                    {
                        IdProthesiste = SelectedProthesiste.Id,
                        Date = NewDate.Value,
                        Achats = NewAchats,
                        SommePayees = (double)somme
                    });
                }
                else
                {
                    Debug.WriteLine($"[SaveCommande] Mise à jour de la commande {_selectedCommande.Id}");
                    _selectedCommande.IdProthesiste = SelectedProthesiste.Id;
                    _selectedCommande.Date = NewDate.Value;
                    _selectedCommande.Achats = NewAchats;
                    _selectedCommande.SommePayees = (double)somme;
                    await _commandeService.UpdateAsync(_selectedCommande);
                }
                CloseModal();
                await LoadCommandes();
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"[SaveCommande] ERREUR: {ex.Message}");
                MessageBox.Show(ex.Message); 
            }
        }

        private void CloseModal()
        {
            Debug.WriteLine("[CloseModal] Fermeture de la modal");
            IsModalOpen = false;
            ProthesisteSearchViewModel.Reset();
        }
    }

    public class CommandeProthesisteDisplayItem
    {
        public int Id { get; set; }
        public string ProthesisteNom { get; set; }
        public DateTime Date { get; set; }
        public string Achats { get; set; }
        public decimal SommePayee { get; set; }
    }
}