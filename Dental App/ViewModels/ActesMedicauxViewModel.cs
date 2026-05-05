using Dental_App.Models;
using Dental_App.Services;
using Dental_App.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;

namespace Dental_App.ViewModels
{
    public class ActesMedicauxViewModel : BindableBase
    {
        private readonly IActeMedicalService _acteService;
        private readonly ILiveSearchService<ActeMedical> _liveSearchService;
        private readonly IAppNotificationService _notificationService; // Add notification service

        // Removed duplicates. Initialized once.
        private ObservableCollection<ActeMedical> _actes = new();
        private ObservableCollection<ActeMedical> _filteredActes = new();
        private string _searchText = string.Empty;
        private bool _isLoading;
        private DelegateCommand? _addActeCommand;
        private DelegateCommand<ActeMedical>? _editActeCommand;

        public ActesMedicauxViewModel(IActeMedicalService acteService, ILiveSearchService<ActeMedical> liveSearchService, IAppNotificationService notificationService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ActesMedicauxViewModel constructor called");
                
                _acteService = acteService ?? throw new ArgumentNullException(nameof(acteService));
                _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));
                _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService)); // Initialize notification service

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
                    _ = FilterActesAsync();
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

        private async Task FilterActesAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredActes = new ObservableCollection<ActeMedical>(Actes);
                });
                return;
            }

            var results = await _liveSearchService.SearchAsync(SearchText, async (searchTerm) => 
            {
                // Note: Normally this would query the service/DB, but in this case the previous implementation 
                // just filtered the in-memory 'Actes' collection. To stick to the same behavior and avoid new repository methods
                // we'll filter the loaded list here, but through the async search service for debouncing
                var filtered = Actes
                    .Where(a => a.Libelle != null && a.Libelle.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                return await Task.FromResult(filtered);
            });

            if (results == null) return; // Search was cancelled

            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredActes = new ObservableCollection<ActeMedical>(results);
            });
        }

        // Keep the original method for initial load or manual triggers (or point it to the async version)
        private void FilterActes()
        {
            _ = FilterActesAsync();
        }

        private void ExecuteAddActe()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExecuteAddActe called");

                var dialogViewModel = new ActeDialogViewModel(null);
                var dialogView = new ActeDialogView { DataContext = dialogViewModel };

                // Create a dialog window
                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                dialogViewModel.CloseDialog = async (result) =>
                {
                    if (result != null)
                    {
                        try
                        {
                            var newActe = await _acteService.CreateAsync(result);
                            Actes.Add(newActe);
                            FilterActes();
                            _notificationService.ShowSuccess("Acte ajouté avec succès.", "Succès"); // Notify success
                        }
                        catch (Exception ex)
                        {
                            _notificationService.ShowError($"Erreur lors de l'ajout: {ex.Message}", "Erreur"); // Notify error
                        }
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Erreur: {ex.Message}", "Erreur"); // Notify error
            }
        }

        private void ExecuteEditActe(ActeMedical acte)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ExecuteEditActe called for: {acte.Libelle}");

                var dialogViewModel = new ActeDialogViewModel(acte);
                var dialogView = new ActeDialogView { DataContext = dialogViewModel };

                // Create a dialog window
                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                dialogViewModel.CloseDialog = async (result) =>
                {
                    if (result != null)
                    {
                        try
                        {
                            var updatedActe = await _acteService.UpdateAsync(result);
                            
                            // Update the collection
                            var index = Actes.IndexOf(acte);
                            if (index >= 0)
                            {
                                Actes[index] = updatedActe;
                            }
                            
                            FilterActes();
                            _notificationService.ShowSuccess("Acte modifié avec succès.", "Succès"); // Notify success
                        }
                        catch (Exception ex)
                        {
                            _notificationService.ShowError($"Erreur lors de la modification: {ex.Message}", "Erreur"); // Notify error
                        }
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Erreur: {ex.Message}", "Erreur"); // Notify error
            }
        }
    }
}
