using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Dental_App.Services
{
    public class ThemeService : IThemeService
    {
        private readonly string _settingsPath;
        private readonly Dictionary<string, Color> _darkColorsSnapshot = new();

        public ThemeService()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dental_App");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "theme.pref");

            // Take snapshot from App resources if available
            var res = Application.Current?.Resources;
            if (res != null)
            {
                CaptureDarkSnapshot(res);
            }
        }

        private void CaptureDarkSnapshot(ResourceDictionary resources)
        {
            var keys = new[]
            {
                "PrimaryBackgroundColor",
                "SecondaryBackgroundColor",
                "CardBackgroundColor",
                "TextPrimaryColor",
                "TextSecondaryColor",
                "BorderColorValue",
                "AccentColorValue",
                "SidebarBackgroundColor"
            };

            foreach (var key in keys)
            {
                try
                {
                    if (!resources.Contains(key))
                        continue;

                    var val = resources[key];

                    if (val is Color c)
                    {
                        _darkColorsSnapshot[key] = c;
                    }
                    else if (val is SolidColorBrush brush)
                    {
                        _darkColorsSnapshot[key] = brush.Color;
                    }
                    else if (val is string s)
                    {
                        var conv = (Color)ColorConverter.ConvertFromString(s);
                        _darkColorsSnapshot[key] = conv;
                    }
                    // else: unsupported resource type, ignore
                }
                catch
                {
                    // Ignore invalid conversions or missing resources
                }
            }
        }

        public void ApplyLightTheme()
        {
            var resources = Application.Current.Resources;
            var lightColors = new Dictionary<string, string>
            {
                { "PrimaryBackgroundColor", "#F8FAFC" },
                { "SecondaryBackgroundColor", "#FFFFFF" },
                { "CardBackgroundColor", "#FFFFFF" },
                { "TextPrimaryColor", "#0F172A" },
                { "TextSecondaryColor", "#64748B" },
                { "BorderColorValue", "#CBD5E1" },
                { "AccentColorValue", "#10B981" },
                { "SidebarBackgroundColor", "#FFFFFF" }
            };

            UpdateThemeColors(resources, lightColors);
            SaveThemePreference(false);
        }

        public void RestoreDarkTheme()
        {
            var resources = Application.Current.Resources;

            // If snapshot empty, try recapturing from resources (in case app started light)
            if (_darkColorsSnapshot.Count == 0)
            {
                CaptureDarkSnapshot(resources);
            }

            if (_darkColorsSnapshot.Count > 0)
            {
                foreach (var kvp in _darkColorsSnapshot)
                {
                    var color = kvp.Value;
                    resources[kvp.Key] = color;

                    var brushKey = kvp.Key.Replace("Color", "");
                    resources[brushKey] = new SolidColorBrush(color);
                }
            }

            SaveThemePreference(true);
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

        public void SaveThemePreference(bool isDark)
        {
            try
            {
                File.WriteAllText(_settingsPath, isDark ? "dark" : "light");
            }
            catch { }
        }

        public bool LoadThemePreference()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var txt = File.ReadAllText(_settingsPath).Trim();
                    return txt == "dark";
                }
            }
            catch { }

            return true; // default dark
        }
    }
}
