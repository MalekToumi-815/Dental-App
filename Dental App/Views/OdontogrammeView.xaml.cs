using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using Dental_App.ViewModels;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for OdontogrammeView.xaml
    /// </summary>
    public partial class OdontogrammeView : UserControl
    {
        public OdontogrammeView()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                LoadOdontogrammeImage();
                LoadToothOverlay();
            };
        }

        private void LoadOdontogrammeImage()
        {
            try
            {
                string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Base Directory: {baseDir}");
                
                string[] possiblePaths = new[]
                {
                    "Assets/Images/OrdontogrameTemplate.png",
                    "./Assets/Images/OrdontogrameTemplate.png",
                    System.IO.Path.Combine(baseDir, "Assets/Images/OrdontogrameTemplate.png"),
                };

                foreach (var path in possiblePaths)
                {
                    string fullPath = System.IO.Path.GetFullPath(path);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Checking path: {fullPath} - Exists: {File.Exists(fullPath)}");
                    
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new System.Uri(fullPath, System.UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            
                            if (FindName("OdontogrammeImage") is Image img)
                            {
                                img.Source = bitmap;
                                System.Diagnostics.Debug.WriteLine($"? IMAGE LOADED SUCCESSFULLY from: {fullPath}");
                            }
                        }
                        catch (Exception bitmapEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load bitmap from {fullPath}: {bitmapEx.Message}");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadOdontogrammeImage: {ex.Message}");
            }
        }

        private void LoadToothOverlay()
        {
            try
            {
                string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Base Directory: {baseDir}");
                
                string[] possiblePaths = new[]
                {
                    "Assets/xaml_chart/OrdontogrameTemplate.xaml",
                    "./Assets/xaml_chart/OrdontogrameTemplate.xaml",
                    System.IO.Path.Combine(baseDir, "Assets/xaml_chart/OrdontogrameTemplate.xaml"),
                    System.IO.Path.Combine(baseDir, @"Assets\xaml_chart\OrdontogrameTemplate.xaml"),
                };

                foreach (var path in possiblePaths)
                {
                    string fullPath = System.IO.Path.GetFullPath(path);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Checking XAML path: {fullPath} - Exists: {File.Exists(fullPath)}");
                    
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            using (var stream = new FileStream(fullPath, FileMode.Open))
                            {
                                var content = XamlReader.Load(stream) as UIElement;
                                if (content != null && FindName("ToothOverlayGrid") is Grid overlayGrid)
                                {
                                    overlayGrid.Children.Add(content);
                                    System.Diagnostics.Debug.WriteLine($"? TOOTH OVERLAY LOADED SUCCESSFULLY from: {fullPath}");
                                }
                            }
                        }
                        catch (Exception xamlEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load XAML from {fullPath}: {xamlEx.Message}");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadToothOverlay: {ex.Message}");
            }
        }

        private void ToothOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point clickPos = e.GetPosition(sender as IInputElement);
                
                // Walk up the visual tree to find the Path element that was clicked
                var hitTest = VisualTreeHelper.HitTest(sender as Visual, clickPos);
                
                if (hitTest?.VisualHit is System.Windows.Shapes.Path path)
                {
                    var toothTag = path.Tag as string;
                    if (!string.IsNullOrEmpty(toothTag))
                    {
                        System.Diagnostics.Debug.WriteLine($"? TOOTH CLICKED: {toothTag}");
                        
                        // Execute the command with the FDI code
                        var viewModel = this.DataContext as OdontogrammeViewModel;
                        if (viewModel?.ToothClickedCommand.CanExecute(toothTag) == true)
                        {
                            viewModel.ToothClickedCommand.Execute(toothTag);
                        }
                    }
                }
            }
        }
    }
}
