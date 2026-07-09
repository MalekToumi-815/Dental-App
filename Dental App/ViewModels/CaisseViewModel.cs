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
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string Nom { get; set; } = string.Empty;
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

        // --- Search (by Nom) ---
        private string _searchNom = string.Empty;
        public string SearchNom
        {
            get { return _searchNom; }
            set
            {
                if (SetProperty(ref _searchNom, value))
                {
                    FilterTransactionsByNom();
                }
            }
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

        private string _newNom = string.Empty;
        public string NewNom
        {
            get { return _newNom; }
            set { SetProperty(ref _newNom, value); }
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

        // --- Pagination state ---
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    NextPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private int _pageSize = 5;
        public int PageSize
        {
            get => _pageSize;
            set { SetProperty(ref _pageSize, value); }
        }

        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            set { SetProperty(ref _totalItems, value); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { SetProperty(ref _totalPages, value); }
        }

        // --- State ---
        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        // --- State for edit ---
        private int? _selectedTransactionId;

        // --- Commands ---
        public DelegateCommand OpenModalCommand { get; }
        public DelegateCommand CloseModalCommand { get; }
        public DelegateCommand SaveTransactionCommand { get; }
        public DelegateCommand<TransactionDisplayItem> EditTransactionCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }

        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }

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

            NextPageCommand = new DelegateCommand(async () =>
            {
                CurrentPage++;
                await LoadPageAsync();
            }, () => CurrentPage < TotalPages);

            PreviousPageCommand = new DelegateCommand(async () =>
            {
                if (CurrentPage > 1) CurrentPage--;
                await LoadPageAsync();
            }, () => CurrentPage > 1);

            Debug.WriteLine("[CaisseViewModel] Initialisé");
            _ = LoadDataAsync();
        }

        /// <summary>
        /// Loads paginated data (or delegated to Nom filter if SearchNom is set)
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("[CaisseViewModel] Chargement des transactions");

                if (!string.IsNullOrWhiteSpace(SearchNom))
                {
                    await FilterTransactionsByNomAsync();
                }
                else
                {
                    await LoadPageAsync();
                }

                await UpdateStatisticsAsync();
                await BuildChartDataAsync();

                Debug.WriteLine($"[CaisseViewModel] Transactions chargées: {Transactions.Count}");
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

        private async Task LoadPageAsync()
        {
            try
            {
                IsLoading = true;

                var caisses = await _caisseService.GetCaisseAsync(CurrentPage, PageSize);

                TotalItems = await _caisseService.GetCaisseCountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));

                Transactions.Clear();
                foreach (var caisse in caisses)
                {
                    Transactions.Add(new TransactionDisplayItem
                    {
                        Id = caisse.Id,
                        Date = caisse.DateDuJour,
                        Nom = caisse.Nom,
                        Type = caisse.IsRevenu ? "Revenu" : "Depense",
                        Montant = caisse.Montant ?? 0
                    });
                }

                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadPageAsync] ERREUR: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement de la page: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

                var maxValue = caisses.GroupBy(c => c.DateDuJour)
                    .SelectMany(g => new[] {
                        g.Where(c => c.IsRevenu).Sum(c => c.Montant ?? 0),
                        g.Where(c => !c.IsRevenu).Sum(c => c.Montant ?? 0)
                    })
                    .DefaultIfEmpty(1)
                    .Max();

                if (maxValue == 0) maxValue = 1;

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
            _selectedTransactionId = null;
            ModalTitle = "Nouvelle Transaction";
            NewDate = DateOnly.FromDateTime(DateTime.Now);
            NewNom = string.Empty;
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
                Debug.WriteLine($"[EditTransaction] Édition de la transaction Id={item.Id}");

                _selectedTransactionId = item.Id;

                ModalTitle = "Modifier la Transaction";
                NewDate = item.Date;
                NewNom = item.Nom;
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
        /// Saves the new transaction to the database (create or update)
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

                if (_selectedTransactionId == null)
                {
                    success = await _caisseService.CreateCaisseAsync(NewMontant, isRevenu, NewNom, NewDate);
                }
                else
                {
                    success = await _caisseService.UpdateCaisseAsync(_selectedTransactionId.Value, NewMontant, isRevenu, NewNom, NewDate!.Value);
                }

                if (success)
                {
                    CloseModal();
                    await LoadDataAsync();
                    string message = _selectedTransactionId == null
                        ? "Transaction enregistrée avec succès."
                        : "Transaction mise à jour avec succès.";
                    _notificationService.ShowSuccess(message, "Succès");
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

            if (string.IsNullOrWhiteSpace(NewNom))
            {
                _notificationService.ShowError("Le nom est obligatoire.", "Validation");
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
        /// Filters transactions by Nom (fire-and-forget)
        /// </summary>
        private void FilterTransactionsByNom()
        {
            _ = FilterTransactionsByNomAsync();
        }

        private async Task FilterTransactionsByNomAsync()
        {
            try
            {
                Debug.WriteLine($"[FilterTransactionsByNom] SearchNom: {SearchNom}");

                Transactions.Clear();

                if (string.IsNullOrWhiteSpace(SearchNom))
                {
                    await LoadPageAsync();
                    return;
                }

                var filtered = await _caisseService.GetCaisseByNomAsync(SearchNom);

                foreach (var caisse in filtered)
                {
                    Transactions.Add(new TransactionDisplayItem
                    {
                        Id = caisse.Id,
                        Date = caisse.DateDuJour,
                        Nom = caisse.Nom,
                        Type = caisse.IsRevenu ? "Revenu" : "Depense",
                        Montant = caisse.Montant ?? 0
                    });
                }

                CurrentPage = 1;
                TotalItems = Transactions.Count;
                TotalPages = 1;
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FilterTransactionsByNom] ERREUR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Clears the search Nom and shows all transactions (paginated)
        /// </summary>
        private void ClearSearch()
        {
            Debug.WriteLine("[ClearSearch] Réinitialisation de la recherche");
            SearchNom = string.Empty;
            CurrentPage = 1;
            _ = LoadPageAsync();
        }
    }
}