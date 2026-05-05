using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Dental_App.Services;

namespace Dental_App.ViewModels
{
    public class GererActeDialogViewModel : BindableBase
    {
        private readonly IActeMedicalService _acteService;
        private readonly IConsultationService _consultationService;
        private readonly IAppNotificationService _notificationService;
        private readonly int _consultationId;
        private string _title = "Gerer les Actes";
        private string _searchText = string.Empty;
        private ObservableCollection<ActeSelectionItem> _allActes;
        private ObservableCollection<ActeSelectionItem> _filteredActes;
        private bool _isLoading;

        public GererActeDialogViewModel(IActeMedicalService acteService, IConsultationService consultationService, IAppNotificationService notificationService, int consultationId)
        {
            _acteService = acteService ?? throw new ArgumentNullException(nameof(acteService));
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _consultationId = consultationId;

            AllActes = new ObservableCollection<ActeSelectionItem>();
            FilteredActes = new ObservableCollection<ActeSelectionItem>();

            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            _ = LoadActesAsync();
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterActes();
                }
            }
        }

        public ObservableCollection<ActeSelectionItem> AllActes
        {
            get => _allActes;
            set => SetProperty(ref _allActes, value);
        }

        public ObservableCollection<ActeSelectionItem> FilteredActes
        {
            get => _filteredActes;
            set => SetProperty(ref _filteredActes, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public Action<bool?> CloseDialog { get; set; }

        private async Task LoadActesAsync()
        {
            try
            {
                IsLoading = true;

                // Load all available actes
                var allActes = await _acteService.GetAllAsync();
                
                // Load actes already associated with this consultation
                var consultationActes = await _consultationService.GetActesByConsultationIdAsync(_consultationId);
                var selectedActeIds = consultationActes.Select(a => a.Id).ToHashSet();

                // Create wrapper items with IsSelected property
                var acteItems = new ObservableCollection<ActeSelectionItem>();
                if (allActes != null)
                {
                    foreach (var acte in allActes)
                    {
                        acteItems.Add(new ActeSelectionItem
                        {
                            Id = acte.Id,
                            Libelle = acte.Libelle,
                            IsSelected = selectedActeIds.Contains(acte.Id)
                        });
                    }
                }

                AllActes = acteItems;
                FilterActes();

                System.Diagnostics.Debug.WriteLine($"Loaded {AllActes.Count} actes, {selectedActeIds.Count} already selected");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading actes: {ex.Message}");
                _notificationService?.ShowError($"Erreur lors du chargement des actes: {ex.Message}", "Erreur");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterActes()
        {
            if (AllActes == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Sort with selected items first
                FilteredActes = new ObservableCollection<ActeSelectionItem>(
                    AllActes.OrderByDescending(a => a.IsSelected).ToList());
            }
            else
            {
                var searchTerm = SearchText.Trim().ToLower();
                var filtered = AllActes
                    .Where(a => a.Libelle != null && a.Libelle.ToLower().Contains(searchTerm))
                    .OrderByDescending(a => a.IsSelected)
                    .ToList();
                FilteredActes = new ObservableCollection<ActeSelectionItem>(filtered);
            }
        }

        private async void ExecuteSave()
        {
            try
            {
                // Get the list of selected actes
                var selectedActes = AllActes
                    .Where(a => a.IsSelected)
                    .Select(a => new ActeMedical { Id = a.Id, Libelle = a.Libelle })
                    .ToList();

                await _consultationService.ClearActesAsync(_consultationId);
                if (selectedActes.Count > 0)
                {
                    var success = await _consultationService.AddActesAsync(_consultationId, selectedActes);
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Added {selectedActes.Count} actes to consultation {_consultationId}");
                    }
                }

                _notificationService?.ShowSuccess("Actes gérés avec succès.", "Succès");
                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExecuteSave: {ex.Message}");
                _notificationService?.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        private void ExecuteCancel() => CloseDialog?.Invoke(false);
    }

    /// <summary>
    /// Wrapper class for acte selection with IsSelected property
    /// </summary>
    public class ActeSelectionItem : BindableBase
    {
        private int _id;
        private string _libelle;
        private bool _isSelected;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Libelle
        {
            get => _libelle;
            set => SetProperty(ref _libelle, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
