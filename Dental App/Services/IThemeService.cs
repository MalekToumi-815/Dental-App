using System.Windows;

namespace Dental_App.Services
{
    public interface IThemeService
    {
        void ApplyLightTheme();
        void RestoreDarkTheme();
        void SaveThemePreference(bool isDark);
        bool LoadThemePreference();
    }
}
