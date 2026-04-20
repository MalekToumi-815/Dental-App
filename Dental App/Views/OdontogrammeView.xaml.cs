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
                
                // Enable InkCanvas for drawing
                if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
                {
                    canvas.EditingMode = System.Windows.Controls.InkCanvasEditingMode.Ink;
                    System.Diagnostics.Debug.WriteLine("? InkCanvas enabled for drawing");
                }
            };
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
                // Create a simple color selection menu
                var colorMenu = new System.Windows.Controls.ContextMenu();
                
                // Define predefined colors
                var colors = new System.Collections.Generic.List<Color>
                {
                    Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow,
                    Colors.Orange, Colors.Purple, Colors.Pink, Colors.Brown,
                    Colors.Black, Colors.Gray, Colors.Cyan, Colors.Magenta
                };

                foreach (var color in colors)
                {
                    var menuItem = new System.Windows.Controls.MenuItem();
                    menuItem.Header = color.ToString();
                    menuItem.Background = new SolidColorBrush(color);
                    menuItem.Foreground = (color.R + color.G + color.B) > 382 ? Brushes.Black : Brushes.White;
                    
                    menuItem.Click += (s, args) =>
                    {
                        if (FindName("FreeDrawCanvas") is System.Windows.Controls.InkCanvas canvas)
                        {
                            canvas.DefaultDrawingAttributes.Color = color;
                            
                            // Update the color preview rectangle
                            if (FindName("ColorPreview") is Rectangle colorRect)
                            {
                                colorRect.Fill = new SolidColorBrush(color);
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"? Color changed to {color}");
                        }
                    };
                    
                    colorMenu.Items.Add(menuItem);
                }

                if (sender is Button btn)
                {
                    colorMenu.PlacementTarget = btn;
                    colorMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    colorMenu.IsOpen = true;
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
