using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using Dental_App.ViewModels;
using System.Windows.Ink;

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
                // Load tooth overlay only in history mode
                UpdateToothOverlayVisibility();

                if (DataContext is OdontogrammeViewModel viewModel)
                {
                    viewModel.LoadInkRequested += ViewModel_LoadInkRequested;
                    viewModel.SaveInkRequested += ViewModel_SaveInkRequested;
                }
                
                // Enable InkCanvas for drawing if active
                if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
                {
                    // Starts as disabled based on ViewModel binding, but ensure setup
                    System.Diagnostics.Debug.WriteLine("? InkCanvas loaded");
                }
            };

            Unloaded += (s, e) =>
            {
                if (DataContext is OdontogrammeViewModel viewModel)
                {
                    viewModel.LoadInkRequested -= ViewModel_LoadInkRequested;
                    viewModel.SaveInkRequested -= ViewModel_SaveInkRequested;
                }
            };
        }

        private void ViewModel_LoadInkRequested(string filePath)
        {
            if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
            {
                canvas.Strokes.Clear();

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    try
                    {
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            if (fs.Length > 0)
                            {
                                canvas.Strokes = new StrokeCollection(fs);
                                System.Diagnostics.Debug.WriteLine($"? Loaded ink from {filePath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load ink: {ex.Message}");
                    }
                }
            }
        }

        private void ViewModel_SaveInkRequested(string filePath)
        {
            if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    // Ensure the directory exists
                    var directory = System.IO.Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        canvas.Strokes.Save(fs);
                        System.Diagnostics.Debug.WriteLine($"? Saved ink to {filePath}");
                        MessageBox.Show("Dessin sauvegardé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to save ink: {ex.Message}");
                    MessageBox.Show($"Erreur lors de la sauvegarde: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetPenMode(object sender, RoutedEventArgs e)
        {
            if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
            {
                canvas.EditingMode = InkCanvasEditingMode.Ink;
                System.Diagnostics.Debug.WriteLine("? Pen mode activated");
            }
        }

        private void SetEraserMode(object sender, RoutedEventArgs e)
        {
            if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
            {
                canvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                System.Diagnostics.Debug.WriteLine("? Eraser mode activated");
            }
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
            {
                canvas.DefaultDrawingAttributes.Width = e.NewValue;
                canvas.DefaultDrawingAttributes.Height = e.NewValue;
                
                // Update the thickness value display
                if (FindName("ThicknessValue") is TextBlock thicknessText)
                {
                    thicknessText.Text = ((int)e.NewValue).ToString();
                }
                
                System.Diagnostics.Debug.WriteLine($"? Thickness changed to: {e.NewValue}");
            }
        }

        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show the color picker dialog
                var colorPickerDialog = new ColorPickerDialog();
                
                // Set current color if available
                if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
                {
                    colorPickerDialog.SelectedColor = canvas.DefaultDrawingAttributes.Color;
                }

                // Create a window to host the dialog
                var window = new Window
                {
                    Title = "Color Picker",
                    Content = colorPickerDialog,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.SingleBorderWindow,
                    Background = new SolidColorBrush(Colors.White),
                };

                if (window.ShowDialog() == true)
                {
                    var selectedColor = colorPickerDialog.SelectedColor;

                    if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas updateCanvas)
                    {
                        updateCanvas.DefaultDrawingAttributes.Color = selectedColor;

                        // Update the color preview rectangle
                        if (FindName("ColorPreview") is Rectangle colorRect)
                        {
                            colorRect.Fill = new SolidColorBrush(selectedColor);
                        }

                        System.Diagnostics.Debug.WriteLine($"? Color changed to #{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening color picker: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ERROR] PickColor_Click: {ex.Message}");
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Clear all drawings?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
                {
                    canvas.Strokes.Clear();
                    System.Diagnostics.Debug.WriteLine("? Canvas cleared");
                }
            }
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

        private void ClearToothOverlay()
        {
            if (FindName("ToothOverlayGrid") is Grid overlayGrid)
            {
                overlayGrid.Children.Clear();
                System.Diagnostics.Debug.WriteLine($"? TOOTH OVERLAY CLEARED");
            }
        }

        private void UpdateToothOverlayVisibility()
        {
            var viewModel = this.DataContext as OdontogrammeViewModel;
            if (viewModel != null)
            {
                if (viewModel.IsHistoryMode)
                {
                    // Load tooth overlay in history mode
                    if (FindName("ToothOverlayGrid") is Grid overlayGrid && overlayGrid.Children.Count == 0)
                    {
                        LoadToothOverlay();
                    }
                }
                else
                {
                    // Clear tooth overlay in edit mode
                    ClearToothOverlay();
                }
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

        private void ToggleSwitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Mark as handled to prevent event from bubbling
            e.Handled = true;
            
            var viewModel = this.DataContext as OdontogrammeViewModel;
            if (viewModel?.ToggleViewModeCommand != null)
            {
                // Execute the toggle command - this will update IsHistoryMode
                // The XAML DataTriggers will handle the animation automatically
                viewModel.ToggleViewModeCommand.Execute();
                
                // Update tooth overlay visibility after animation completes (300ms)
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = System.TimeSpan.FromMilliseconds(350);
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    UpdateToothOverlayVisibility();
                };
                timer.Start();
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unload the current tooth overlay if needed
            if (FindName("ToothOverlayGrid") is Grid overlayGrid)
            {
                overlayGrid.Children.Clear();
            }

            // Reload the tooth overlay based on the new mode
            LoadToothOverlay();
        }
    }
}
