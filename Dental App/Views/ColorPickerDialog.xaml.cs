using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dental_App.Views
{
    /// <summary>
    /// Interaction logic for ColorPickerDialog.xaml
    /// </summary>
    public partial class ColorPickerDialog : UserControl
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPickerDialog), 
                new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        private double _currentHue = 0; // 0-360
        private double _currentSaturation = 1; // 0-1
        private double _currentValue = 1; // 0-1

        public ColorPickerDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => InitializeColorPicker();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = d as ColorPickerDialog;
            if (picker != null)
            {
                var color = (Color)e.NewValue;
                picker.UpdateColorFromRGB(color);
            }
        }

        private void InitializeColorPicker()
        {
            // Create hue gradient (full spectrum)
            var hueGradient = new LinearGradientBrush();
            hueGradient.StartPoint = new Point(0, 0);
            hueGradient.EndPoint = new Point(1, 0);

            // Add color stops for the full spectrum
            var hues = new[] { Colors.Red, Colors.Yellow, Colors.Lime, Colors.Cyan, Colors.Blue, Colors.Magenta, Colors.Red };
            for (int i = 0; i < hues.Length; i++)
            {
                hueGradient.GradientStops.Add(new GradientStop(hues[i], (double)i / (hues.Length - 1)));
            }

            HueGradient.Fill = hueGradient;
            UpdateColorSquare();
        }

        private void HueGradient_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectHueFromPosition(e.GetPosition(HueGradient));
        }

        private void HueGradient_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SelectHueFromPosition(e.GetPosition(HueGradient));
            }
        }

        private void SelectHueFromPosition(Point position)
        {
            if (HueGradient == null || HueGradient.ActualWidth == 0)
                return;
                
            double huePercent = position.X / HueGradient.ActualWidth;
            _currentHue = huePercent * 360;
            UpdateColorSquare();
        }

        private void UpdateColorSquare()
        {
            // Create saturation-value gradient
            var squareGradient = new LinearGradientBrush();
            squareGradient.StartPoint = new Point(0, 1);
            squareGradient.EndPoint = new Point(1, 0);

            // Base hue color
            var hueColor = HsvToRgb(_currentHue, 1, 1);

            // Horizontal: white to hue (saturation)
            var leftGradient = new LinearGradientBrush();
            leftGradient.StartPoint = new Point(0, 0);
            leftGradient.EndPoint = new Point(1, 0);
            leftGradient.GradientStops.Add(new GradientStop(Colors.White, 0));
            leftGradient.GradientStops.Add(new GradientStop(hueColor, 1));

            // Vertical: color to black (value)
            var finalGradient = new LinearGradientBrush();
            finalGradient.StartPoint = new Point(0, 0);
            finalGradient.EndPoint = new Point(0, 1);
            finalGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
            finalGradient.GradientStops.Add(new GradientStop(Colors.Black, 1));

            // Create a visual brush stack effect (we'll use a different approach)
            // For simplicity, we'll use a linear gradient that approximates the 2D color space
            var approximateGradient = new LinearGradientBrush();
            approximateGradient.StartPoint = new Point(0, 1);
            approximateGradient.EndPoint = new Point(1, 0);

            // Create corner colors
            var topLeft = Colors.White;
            var topRight = hueColor;
            var bottomLeft = Colors.Black;
            var bottomRight = HsvToRgb(_currentHue, 1, 0);

            // Approximate with stops
            approximateGradient.GradientStops.Add(new GradientStop(hueColor, 0));
            approximateGradient.GradientStops.Add(new GradientStop(Colors.Black, 1));

            if (ColorSquare != null)
            {
                ColorSquare.Fill = leftGradient;
            }
            
            UpdateSelectedColor();
        }

        private void ColorSquare_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectColorFromPosition(e.GetPosition(ColorSquare));
        }

        private void ColorSquare_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SelectColorFromPosition(e.GetPosition(ColorSquare));
            }
        }

        private void SelectColorFromPosition(Point position)
        {
            if (ColorSquare == null || ColorSquare.ActualWidth == 0 || ColorSquare.ActualHeight == 0)
                return;
            
            // Saturation: horizontal (0 to 1)
            _currentSaturation = position.X / ColorSquare.ActualWidth;
            _currentSaturation = System.Math.Max(0, System.Math.Min(1, _currentSaturation));

            // Value: vertical inverted (1 to 0, top to bottom)
            _currentValue = 1 - (position.Y / ColorSquare.ActualHeight);
            _currentValue = System.Math.Max(0, System.Math.Min(1, _currentValue));

            UpdateSelectedColor();
        }

        private void ValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currentValue = e.NewValue / 100.0;
            UpdateSelectedColor();
        }

        private void UpdateSelectedColor()
        {
            SelectedColor = HsvToRgb(_currentHue, _currentSaturation, _currentValue);
            
            // Add null checks to prevent NullReferenceException
            if (PreviewRect != null)
            {
                PreviewRect.Fill = new SolidColorBrush(SelectedColor);
            }
            
            if (ColorHexText != null)
            {
                ColorHexText.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
            }
        }

        private void UpdateColorFromRGB(Color color)
        {
            var (h, s, v) = RgbToHsv(color.R, color.G, color.B);
            _currentHue = h;
            _currentSaturation = s;
            _currentValue = v;
            ValueSlider.Value = _currentValue * 100;
            UpdateColorSquare();
        }

        private Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - System.Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;

            if (h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return Color.FromArgb(255,
                (byte)System.Math.Round((r + m) * 255),
                (byte)System.Math.Round((g + m) * 255),
                (byte)System.Math.Round((b + m) * 255));
        }

        private (double h, double s, double v) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = System.Math.Max(rd, System.Math.Max(gd, bd));
            double min = System.Math.Min(rd, System.Math.Min(gd, bd));
            double delta = max - min;

            double h = 0;
            if (delta != 0)
            {
                if (max == rd)
                    h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd)
                    h = 60 * (((bd - rd) / delta) + 2);
                else
                    h = 60 * (((rd - gd) / delta) + 4);
            }

            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}
