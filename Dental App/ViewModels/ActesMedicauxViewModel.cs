using Dental_App.Models;
using Dental_App.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class ActesMedicauxViewModel : BindableBase
    {
        private readonly IActeMedicalService _acteService;

        // Removed duplicates. Initialized once.
        private ObservableCollection<ActeMedical> _actes = new();
        private ObservableCollection<ActeMedical> _filteredActes = new();
        private string _searchText = string.Empty;
        private bool _isLoading;
        private DelegateCommand? _addActeCommand;
        private DelegateCommand<ActeMedical>? _editActeCommand;

        public ActesMedicauxViewModel(IActeMedicalService acteService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ActesMedicauxViewModel constructor called");
                
                if (acteService == null)
                {
                    throw new ArgumentNullException(nameof(acteService), "IActeMedicalService is not registered in the container");
                }

                _acteService = acteService;

                // Initialize Commands
                _addActeCommand = new DelegateCommand(ExecuteAddActe);
                _editActeCommand = new DelegateCommand<ActeMedical>(ExecuteEditActe);

                System.Diagnostics.Debug.WriteLine("ActesMedicauxViewModel initialization successful");

                // Start loading data without blocking the constructor
                _ = LoadActesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ActesMedicauxViewModel constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"ViewModel initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Properties
        public ObservableCollection<ActeMedical> Actes
        {
            get => _actes;
            set => SetProperty(ref _actes, value);
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

        public ObservableCollection<ActeMedical> FilteredActes
        {
            get => _filteredActes;
            set => SetProperty(ref _filteredActes, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public DelegateCommand AddActeCommand
        {
            get => _addActeCommand ?? new DelegateCommand(ExecuteAddActe);
        }

        public DelegateCommand<ActeMedical> EditActeCommand
        {
            get => _editActeCommand ?? new DelegateCommand<ActeMedical>(ExecuteEditActe);
        }
        #endregion

        private async Task LoadActesAsync()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("LoadActesAsync started");
                
                var data = await _acteService.GetAllAsync();

                // Switch to UI Thread only for the final update
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Actes = new ObservableCollection<ActeMedical>(data ?? new List<ActeMedical>());
                    FilterActes();
                    IsLoading = false;
                    System.Diagnostics.Debug.WriteLine($"LoadActesAsync completed. Loaded {Actes.Count} actes");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                IsLoading = false;
            }
        }

        private void FilterActes()
        {
            if (Actes == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredActes = new ObservableCollection<ActeMedical>(Actes);
            }
            else
            {
                var filtered = Actes
                    .Where(a => a.Libelle != null && a.Libelle.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                FilteredActes = new ObservableCollection<ActeMedical>(filtered);
            }
        }

        private void ExecuteAddActe() { /* Implementation */ }
        private void ExecuteEditActe(ActeMedical acte) { /* Implementation */ }
    }
}