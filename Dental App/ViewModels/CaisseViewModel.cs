using Dental_App.Models;
using Dental_App.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dental_App.ViewModels
{
    /// <summary>
    /// Display item for transactions in the DataGrid
    /// </summary>
    public class TransactionDisplayItem
    {
        public DateOnly Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Montant { get; set; }
    }

    /// <summary>
	/// Display item for chart bar showing revenue/expense evolution
	/// </summary>
	public class CaisseChartBarItem
    {
        public string DayName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public double RevenueBarHeight { get; set; }
        public double ExpenseBarHeight { get; set; }
    }

    public class CaisseViewModel : BindableBase
    {
        private readonly ICaisseService _caisseService;
        private readonly IAppNotificationService _notificationService;

        // --- Collections ---
        private ObservableCollection<TransactionDisplayItem> _transactions = new();
        public ObservableCollection<TransactionDisplayItem> Transactions
        {
            get { return _transactions; }
            set { SetProperty(ref _transactions, value); }
        }

        private ObservableCollection<CaisseChartBarItem> _chartData = new();
        public ObservableCollection<CaisseChartBarItem> ChartData
        {
            get { return _chartData; }
            set { SetProperty(ref _chartData, value); }
        }

        // --- Search ---
        private DateOnly? _searchDate;
        public DateOnly? SearchDate
        {
            get { return _searchDate; }
            set
            {
                if (SetProperty(ref _searchDate, value))
                {
                    FilterTransactionsByDate();
                }
            }
        }

        private ObservableCollection<TransactionDisplayItem> _allTransactions = new();
        public ObservableCollection<TransactionDisplayItem> AllTransactions
        {
            get { return _allTransactions; }
            set { SetProperty(ref _allTransactions, value); }
        }

        // --- Statistics ---
        private decimal _totalRevenus;
        public decimal TotalRevenus
        {
            get { return _totalRevenus; }
            set { SetProperty(ref _totalRevenus, value); }
        }

        private decimal _totalDepenses;
        public decimal TotalDepenses
        {
            get { return _totalDepenses; }
            set { SetProperty(ref _totalDepenses, value); }
        }

        private decimal _soldeNet;
        public decimal SoldeNet
        {
            get { return _soldeNet; }
            set { SetProperty(ref _soldeNet, value); }
        }

        // --- Modal ---
        private bool _isModalVisible;
        public bool IsModalVisible
        {
            get { return _isModalVisible; }
            set { SetProperty(ref _isModalVisible, value); }
        }

        private string _modalTitle = "Nouvelle Transaction";
        public string ModalTitle
        {
            get { return _modalTitle; }
            set { SetProperty(ref _modalTitle, value); }
        }

        private DateOnly? _newDate;
        public DateOnly? NewDate
        {
            get { return _newDate; }
            set { SetProperty(ref _newDate, value); }
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get { return _newDescription; }
            set { SetProperty(ref _newDescription, value); }
        }

        private string _newType = string.Empty;
        public string NewType
        {
            get { return _newType; }
            set { SetProperty(ref _newType, value); }
        }

        private decimal _newMontant;
        public decimal NewMontant
        {
            get { return _newMontant; }
            set { SetProperty(ref _newMontant, value); }
        }

        // --- State ---
        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        // --- State for edit ---
        private Caisse _selectedTransaction;

        // --- Commands ---
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveTransactionCommand { get; }
        public DelegateCommand<TransactionDisplayItem> EditTransactionCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }

        public CaisseViewModel(ICaisseService caisseService, IAppNotificationService notificationService)
        {
            _caisseService = caisseService ?? throw new ArgumentNullException(nameof(caisseService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            IsModalVisible = false;

            OpenModalCommand = new DelegateCommand(OpenModal);
            CloseModalCommand = new DelegateCommand(CloseModal);
            SaveTransactionCommand = new DelegateCommand(SaveTransaction);
            EditTransactionCommand = new DelegateCommand<TransactionDisplayItem>(EditTransaction);
            ClearSearchCommand = new DelegateCommand(ClearSearch);

            Debug.WriteLine("[CaisseViewModel] Initialisé");
            _ = LoadDataAsync();
        }

        /// <summary>
        /// Loads all transactions and calculates statistics
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("[CaisseViewModel] Chargement des transactions");

                // Get all transactions
                var caisses = await _caisseService.GetAllCaisseAsync();
                Transactions.Clear();
                AllTransactions.Clear();

                foreach (var caisse in caisses)
                {
                    var transaction = new TransactionDisplayItem
                    {
                        Date = caisse.DateDuJour,
                        Description = caisse.IsRevenu ? "Revenu" : "Depense",
                        Type = caisse.IsRevenu ? "Revenu" : "Depense",
                        Montant = caisse.Montant ?? 0
                    };
                    AllTransactions.Add(transaction);
                    Transactions.Add(transaction);
                }

                Debug.WriteLine($"[CaisseViewModel] AllTransactions: {AllTransactions.Count}, Transactions affichées: {Transactions.Count}");

                // Calculate totals
                await UpdateStatisticsAsync();

                // Build chart data for last 7 days
                await BuildChartDataAsync();

                Debug.WriteLine($"[CaisseViewModel] {Transactions.Count} transactions chargées");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadDataAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates total revenue, expenses, and balance
        /// </summary>
        private async Task UpdateStatisticsAsync()
        {
            try
            {
                var (totalRevenu, totalDepense) = await _caisseService.GetTodaySummaryAsync();
                TotalRevenus = totalRevenu;
                TotalDepenses = totalDepense;
                SoldeNet = totalRevenu - totalDepense;

                Debug.WriteLine($"[UpdateStatisticsAsync] Revenus: {TotalRevenus}, Depenses: {TotalDepenses}, Solde: {SoldeNet}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateStatisticsAsync] ERREUR: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds chart data showing revenue and expense evolution for the last 7 days
        /// </summary>
        private async Task BuildChartDataAsync()
        {
            try
            {
                ChartData.Clear();
                var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-6));
                var endDate = DateOnly.FromDateTime(DateTime.Today);

                var caisses = await _caisseService.GetCaisseByDateRangeAsync(startDate, endDate);

                // Find max values for scaling
                var maxValue = caisses.GroupBy(c => c.DateDuJour)
                    .SelectMany(g => new[] { 
                        g.Where(c => c.IsRevenu).Sum(c => c.Montant ?? 0),
                        g.Where(c => !c.IsRevenu).Sum(c => c.Montant ?? 0)
                    })
                    .DefaultIfEmpty(1)
                    .Max();

                if (maxValue == 0) maxValue = 1;

                // Build chart data for each day
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i));
                    var dayCaisses = caisses.Where(c => c.DateDuJour == date).ToList();

                    var revenue = dayCaisses.Where(c => c.IsRevenu).Sum(c => c.Montant ?? 0);
                    var expense = dayCaisses.Where(c => !c.IsRevenu).Sum(c => c.Montant ?? 0);

                    ChartData.Add(new CaisseChartBarItem
                    {
                        DayName = date.ToString("ddd"),
                        Revenue = revenue,
                        Expense = expense,
                        RevenueBarHeight = Math.Max((double)revenue / (double)maxValue * 150, 2),
                        ExpenseBarHeight = Math.Max((double)expense / (double)maxValue * 150, 2)
                    });
                }

                Debug.WriteLine($"[BuildChartDataAsync] Données du graphique construites: {ChartData.Count} jours");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BuildChartDataAsync] ERREUR: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the modal for creating a new transaction
        /// </summary>
        private void OpenModal()
        {
            Debug.WriteLine("[OpenModal] Ouverture de la modal");
            _selectedTransaction = null;
            ModalTitle = "Nouvelle Transaction";
            NewDate = DateOnly.FromDateTime(DateTime.Now);
            NewDescription = string.Empty;
            NewType = string.Empty;
            NewMontant = 0;
            IsModalVisible = true;
        }

        /// <summary>
        /// Closes the modal
        /// </summary>
        public void CloseModal()
        {
            Debug.WriteLine("[CloseModal] Fermeture de la modal");
            IsModalVisible = false;
        }

        /// <summary>
        /// Edit an existing transaction
        /// </summary>
        private void EditTransaction(TransactionDisplayItem item)
        {
            if (item == null) return;

            try
            {
                Debug.WriteLine($"[EditTransaction] Édition de la transaction du {item.Date}");
                
                // Create a temporary Caisse object for editing
                _selectedTransaction = new Caisse
                {
                    DateDuJour = item.Date,
                    IsRevenu = item.Type == "Revenu",
                    Montant = item.Montant
                };

                ModalTitle = "Modifier la Transaction";
                NewDate = item.Date;
                NewDescription = item.Description;
                NewType = item.Type;
                NewMontant = item.Montant;
                IsModalVisible = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditTransaction] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves the new transaction to the database
        /// </summary>
        private async void SaveTransaction()
        {
            if (!ValidateForm())
                return;

            try
            {
                IsLoading = true;
                Debug.WriteLine("[SaveTransaction] Enregistrement de la transaction");

                bool isRevenu = NewType == "Revenu";
                bool success;
                if (isRevenu) {
                    success = await _caisseService.AddOrUpdateCaisseAsync(
                        NewMontant,
                        true,
                        NewDate
                    );
                }
                else
                {
                    success = await _caisseService.AddOrUpdateCaisseAsync(
                    NewMontant,
                    false,
                    NewDate
                );
                }

                if (success)
                {
                    CloseModal();
                    await LoadDataAsync();
                    string message = _selectedTransaction == null
                        ? "Transaction enregistrée avec succès."
                        : "Transaction mise à jour avec succès.";
                    _notificationService.ShowSuccess("Transaction sauvegarder avec succès.", "Succès");
                }
                else
                {
                    MessageBox.Show("Erreur lors de l'enregistrement de la transaction.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveTransaction] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Validates the transaction form
        /// </summary>
        private bool ValidateForm()
        {
            if (!NewDate.HasValue)
            {
                _notificationService.ShowError("La date est obligatoire.", "Validation");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewType))
            {
                _notificationService.ShowError("Le type de transaction est obligatoire.", "Validation");
                return false;
            }

            if (NewMontant <= 0)
            {
                _notificationService.ShowError("Le montant doit être supérieur à 0.", "Validation");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Filters transactions by the selected date
        /// </summary>
        private void FilterTransactionsByDate()
        {
            try
            {
                Debug.WriteLine($"[FilterTransactionsByDate] SearchDate: {SearchDate}, AllTransactions count: {AllTransactions.Count}");

                if (!SearchDate.HasValue)
                {
                    Debug.WriteLine("[FilterTransactionsByDate] SearchDate is null - showing all transactions");
                    Transactions.Clear();
                    foreach (var transaction in AllTransactions)
                    {
                        Transactions.Add(transaction);
                    }
                    Debug.WriteLine($"[FilterTransactionsByDate] Affichage de {Transactions.Count} transactions");
                    return;
                }

                var selectedDate = SearchDate.Value;
                Debug.WriteLine($"[FilterTransactionsByDate] Recherche pour la date: {selectedDate:yyyy-MM-dd}");
                
                var filteredTransactions = AllTransactions
                    .Where(t => t.Date.Year == selectedDate.Year && 
                                t.Date.Month == selectedDate.Month && 
                                t.Date.Day == selectedDate.Day)
                    .ToList();

                Debug.WriteLine($"[FilterTransactionsByDate] {filteredTransactions.Count} transactions trouvées");

                Transactions.Clear();
                
                if (filteredTransactions.Count == 0)
                {
                    Debug.WriteLine($"[FilterTransactionsByDate] Aucune transaction pour le {selectedDate:yyyy-MM-dd}");
                    // Affiche rien si aucune transaction n'existe pour cette date
                }
                else
                {
                    foreach (var transaction in filteredTransactions)
                    {
                        Transactions.Add(transaction);
                    }
                    Debug.WriteLine($"[FilterTransactionsByDate] {filteredTransactions.Count} transactions affichées");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FilterTransactionsByDate] ERREUR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Clears the search date and shows all transactions
        /// </summary>
        private void ClearSearch()
        {
            Debug.WriteLine("[ClearSearch] Réinitialisation de la recherche");
            SearchDate = null;
            Debug.WriteLine("[ClearSearch] Recherche réinitialisée");
        }
    }
}
