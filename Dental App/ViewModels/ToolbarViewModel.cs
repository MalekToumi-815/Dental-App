using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Dental_App.ViewModels
{
    public class ToolbarViewModel : BindableBase
    {
        private readonly CultureInfo _frenchCulture = new CultureInfo("fr-FR");
        private string _todayDate;
        private bool _isDarkMode = true;
        private Geometry _themeIconGeometry;

        public ToolbarViewModel()
        {
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
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
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

        private void ApplyDarkTheme()
        {
            var resources = Application.Current.Resources;
            var darkColors = new Dictionary<string, string>
            {
                { "PrimaryBackgroundColor", "#0D1117" },
                { "SecondaryBackgroundColor", "#0F172A" },
                { "CardBackgroundColor", "#0F172A" },
                { "TextPrimaryColor", "#FFFFFF" },
                { "TextSecondaryColor", "#94A3B8" },
                { "BorderColorValue", "#1E293B" },
                { "AccentColorValue", "#2ECC71" },
                { "SidebarBackgroundColor", "#101926" }
            };

            UpdateThemeColors(resources, darkColors);
        }

        private void ApplyLightTheme()
        {
            var resources = Application.Current.Resources;
            var lightColors = new Dictionary<string, string>
            {
                { "PrimaryBackgroundColor", "#F8FAFC" },   // A cleaner, modern "off-white" (Slate 50)
{ "SecondaryBackgroundColor", "#FFFFFF" },
{ "CardBackgroundColor", "#FFFFFF" },
{ "TextPrimaryColor", "#0F172A" },        // Deepest Slate (900) for high contrast
{ "TextSecondaryColor", "#64748B" },      // Muted Slate (500) for labels
{ "BorderColorValue", "#CBD5E1" },        // More defined borders (Slate 200)
{ "AccentColorValue", "#10B981" },        // A slightly more "Emerald" green (Modern SaaS feel)
{ "SidebarBackgroundColor", "#FFFFFF" }    // Keep sidebar white for a "clean-cut" look
            };

            UpdateThemeColors(resources, lightColors);
        }

        private void UpdateThemeColors(ResourceDictionary resources, Dictionary<string, string> colorMap)
        {
            foreach (var kvp in colorMap)
            {
                var colorKey = kvp.Key;
                var colorValue = (Color)ColorConverter.ConvertFromString(kvp.Value);
                
                resources[colorKey] = colorValue;
                
                var brushKey = colorKey.Replace("Color", "");
                if (resources.Contains(brushKey))
                {
                    // Create new unfrozen brush instead of modifying frozen one
                    var newBrush = new SolidColorBrush(colorValue);
                    resources[brushKey] = newBrush;
                }
            }
        }

        private void CloseApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
