using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class SidebarViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private string _activeView = "DashboardView";

        public SidebarViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(Navigate);
        }

        public DelegateCommand<string> NavigateCommand { get; }

        public string ActiveView
        {
            get => _activeView;
            set => SetProperty(ref _activeView, value);
        }

        private void Navigate(string navigatePath)
        {
            if (string.IsNullOrEmpty(navigatePath))
            {
                System.Diagnostics.Debug.WriteLine("Navigation path is null or empty");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to navigate to: {navigatePath}");

                // Update active view immediately (optimistic update)
                ActiveView = navigatePath;

                // Create a Uri for the navigation
                Uri navigationUri = new Uri(navigatePath, UriKind.Relative);

                _regionManager.RequestNavigate(
                    "ContentRegion",
                    navigationUri,
                    NavigationCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Navigation failed: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigationCallback(NavigationResult result)
        {
            if (result != null)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation callback received");
            }
        }
    }
}