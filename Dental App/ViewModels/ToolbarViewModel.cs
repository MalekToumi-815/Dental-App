using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Dental_App.Services;

namespace Dental_App.ViewModels
{
    public class ToolbarViewModel : BindableBase
    {
        private readonly CultureInfo _frenchCulture = new CultureInfo("fr-FR");
        private string _todayDate;
        private bool _isDarkMode = true;
        private Geometry _themeIconGeometry;
        private readonly IThemeService _themeService;

        public ToolbarViewModel(IThemeService themeService)
        {
            _themeService = themeService;

            TodayDate = DateTime.Now.ToString("dddd d MMMM yyyy", _frenchCulture);
            ToggleThemeCommand = new DelegateCommand(ToggleTheme);
            CloseApplicationCommand = new DelegateCommand(CloseApplication);
            // Initialize with a default and then update after dispatcher queue processes
            UpdateThemeIcon();
            // Also schedule an update to ensure resources are available
            Application.Current?.Dispatcher?.BeginInvoke(new Action(UpdateThemeIcon), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        public string TodayDate
        {
            get => _todayDate;
            set => SetProperty(ref _todayDate, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        public Geometry ThemeIconGeometry
        {
            get => _themeIconGeometry;
            set => SetProperty(ref _themeIconGeometry, value);
        }

        public DelegateCommand ToggleThemeCommand { get; }

        public DelegateCommand CloseApplicationCommand { get; }

        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;

            if (IsDarkMode)
            {
                // Restore the original dark theme snapshot
                _themeService.RestoreDarkTheme();
            }
            else
            {
                // Apply the lighter color scheme
                _themeService.ApplyLightTheme();
            }

            UpdateThemeIcon();
        }

        private void UpdateThemeIcon()
        {
            try
            {
                if (IsDarkMode)
                {
                    // In dark mode, show moon icon to switch to light mode
                    ThemeIconGeometry = (Geometry)Application.Current.Resources["IconMoon"];
                }
                else
                {
                    // In light mode, show sun icon to switch to dark mode
                    ThemeIconGeometry = (Geometry)Application.Current.Resources["IconSun"];
                }
            }
            catch
            {
                // Resources not yet available, will retry after toggle
            }
        }

        private void CloseApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
