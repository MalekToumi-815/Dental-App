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
        private readonly ILiveSearchService<Prothesiste> _liveSearchService;
        private readonly IAppNotificationService _notificationService; // Add notification service

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
                    _ = FilterProthesistesAsync();
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

        // Orders (commands) modal
        private bool _isOrdersModalOpen;
        public bool IsOrdersModalOpen
        {
            get => _isOrdersModalOpen;
            set => SetProperty(ref _isOrdersModalOpen, value);
        }

        private string _ordersModalTitle = "Commandes";
        public string OrdersModalTitle
        {
            get => _ordersModalTitle;
            set => SetProperty(ref _ordersModalTitle, value);
        }

        private ObservableCollection<int> _orderYears = new ObservableCollection<int>();
        public ObservableCollection<int> OrderYears
        {
            get => _orderYears;
            set => SetProperty(ref _orderYears, value);
        }

        public class MonthItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private ObservableCollection<MonthItem> _months = new ObservableCollection<MonthItem>();
        public ObservableCollection<MonthItem> Months
        {
            get => _months;
            set => SetProperty(ref _months, value);
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    SelectedYearText = value.ToString();
                    _ = LoadOrdersForSelectedPeriodAsync();
                }
            }
        }

        private string _selectedYearText = string.Empty;
        public string SelectedYearText
        {
            get => _selectedYearText;
            set
            {
                if (SetProperty(ref _selectedYearText, value))
                {
                    if (int.TryParse(value, out int year) &&
                        year > 1900 &&
                        year < 3000)
                    {
                        if (SelectedYear != year)
                        {
                            SelectedYear = year;
                        }
                    }
                }
            }
        }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                {
                    _ = LoadOrdersForSelectedPeriodAsync();
                }
            }
        }

        private ObservableCollection<string> _orders = new ObservableCollection<string>();
        public ObservableCollection<string> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        private int _ordersCount;
        public int OrdersCount
        {
            get => _ordersCount;
            set => SetProperty(ref _ordersCount, value);
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
        private Prothesiste _currentProthesisteForOrders;

        // Commands
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand<ProthesisteDisplayItem> EditCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }

        // New commands for orders modal
        public DelegateCommand<ProthesisteDisplayItem> OpenOrdersCommand { get; }
        public DelegateCommand CloseOrdersCommand { get; }

        public ProthesisteViewModel(IProthesisteService prothesisteService, ILiveSearchService<Prothesiste> liveSearchService, IAppNotificationService notificationService)
        {
            _prothesisteService = prothesisteService ?? throw new ArgumentNullException(nameof(prothesisteService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService)); // Initialize notification service

            Prosthesists = new ObservableCollection<ProthesisteDisplayItem>();

            // Initialize commands
            OpenModalCommand = new DelegateCommand(OpenModal);
            CloseModalCommand = new DelegateCommand(CloseModal);
            SaveCommand = new DelegateCommand(SaveProthesiste);
            EditCommand = new DelegateCommand<ProthesisteDisplayItem>(EditProthesiste);
            ClearSearchCommand = new DelegateCommand(ClearSearch);

            OpenOrdersCommand = new DelegateCommand<ProthesisteDisplayItem>(OpenOrders);
            CloseOrdersCommand = new DelegateCommand(CloseOrders);

            // Initialize months
            InitializeMonths();

            // Load data
            LoadProthesistes();
        }

        private void InitializeMonths()
        {
            var names = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
            Months.Clear();
            for (int i = 0; i < 12; i++)
            {
                Months.Add(new MonthItem { Id = i + 1, Name = names[i] });
            }
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
        /// Public refresh method to reload prothésistes when view becomes visible or navigated to.
        /// </summary>
        public void Refresh()
        {
            // Keep simple: reload the list
            LoadProthesistes();
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

                var results = await _liveSearch_service_wrapper(SearchText);

                if (results == null) return; // Search was cancelled

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
                        NbCommandes = p.CommandeProthesistes?.Count ?? 0
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // helper to keep original logic localized
        private async Task<System.Collections.Generic.List<Prothesiste>> _liveSearch_service_wrapper(string searchText)
        {
            var results = await _liveSearchService.SearchAsync(searchText, async (searchTerm) =>
            {
                // Si la saisie contient uniquement des chiffres -> Recherche par téléphone
                if (searchTerm.All(char.IsDigit) || searchTerm.All(c => char.IsDigit(c) || c == ' ' || c == '-' || c == '+'))
                {
                    return await _prothesisteService.GetByPhoneAsync(searchTerm);
                }
                // Sinon -> Recherche par nom
                else
                {
                    return await _prothesisteService.GetByNameAsync(searchTerm);
                }
            });

            // SearchAsync may return IEnumerable<T> or null if cancelled - convert to List<T> while preserving null
            return results?.ToList();
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
                _notification_service_wrapper("Le nom du prothésiste est requis.", "Validation");
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
                    _notification_service_wrapper("Prothésiste créé avec succès.", "Succès");
                }
                else
                {
                    // Update existing
                    _selectedProthesiste.Nom = NewNom.Trim();
                    _selectedProthesiste.Adresse = string.IsNullOrWhiteSpace(NewAdresse) ? null : NewAdresse.Trim();
                    _selectedProthesiste.Tel = string.IsNullOrWhiteSpace(NewTelephone) ? null : NewTelephone.Trim();

                    await _prothesisteService.UpdateAsync(_selectedProthesiste);
                    _notification_service_wrapper("Prothésiste mis à jour avec succès.", "Succès");
                }

                CloseModal();
                LoadProthesistes();
            }
            catch (Exception ex)
            {
                _notification_service_wrapper($"Erreur lors de l'enregistrement: {ex.Message}", "Erreur");
            }
        }

        private void _notification_service_wrapper(string message, string title)
        {
            try { _notificationService.ShowSuccess(message, title); }
            catch { /* ignore */ }
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

        /// <summary>
        /// Open the orders modal for a given prothésiste
        /// </summary>
        private async void OpenOrders(ProthesisteDisplayItem item)
        {

            if (item == null) return;
            try
            {
                _currentProthesisteForOrders = await _prothesisteService.GetByIdAsync(item.Id);
                OrdersModalTitle = $"Commandes - {item.Nom}";

                // Populate years from commands
                OrderYears.Clear();
                var years = _currentProthesisteForOrders.CommandeProthesistes
                    .Where(c => c.Date.HasValue)
                    .Select(c => c.Date.Value.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList();

                if (!years.Any())
                {
                    var now = DateTime.Now.Year;
                    for (int y = now; y >= now - 3; y--) OrderYears.Add(y);
                    SelectedYear = DateTime.Now.Year;
                }
                else
                {
                    foreach (var y in years) OrderYears.Add(y);
                    SelectedYear = years.First();
                }

                // Default month to current month or most recent in data
                SelectedMonth = DateTime.Now.Month;

                Orders.Clear();
                OrdersCount = 0;

                IsOrdersModalOpen = true;

                await LoadOrdersForSelectedPeriodAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des commandes: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseOrders()
        {
            IsOrdersModalOpen = false;
            Orders.Clear();
            OrdersCount = 0;
            _currentProthesisteForOrders = null;
        }

        private Task LoadOrdersForSelectedPeriodAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_currentProthesisteForOrders == null) return;

                    var list = _currentProthesisteForOrders.CommandeProthesistes
                        .Where(c => c.Date.HasValue && c.Date.Value.Year == SelectedYear && c.Date.Value.Month == SelectedMonth)
                        .OrderByDescending(c => c.Date)
                        .ToList();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Orders.Clear();
                        foreach (var c in list)
                        {
                            var name = string.IsNullOrWhiteSpace(c.Achats) ? $"Commande #{c.Id}" : c.Achats;
                            Orders.Add(name);
                        }
                        OrdersCount = Orders.Count;
                    });
                }
                catch (Exception ex)
                {
                    // ignore or log
                }
            });
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

