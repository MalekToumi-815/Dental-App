using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;

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
                try
                {
                    // Try multiple paths
                    string[] possiblePaths = new[]
                    {
                        "Assets/Images/OrdontogrameTemplate.png",
                        "./Assets/Images/OrdontogrameTemplate.png",
                        "../Assets/Images/OrdontogrameTemplate.png",
                        System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets/Images/OrdontogrameTemplate.png"),
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            var bitmap = new BitmapImage(new System.Uri(System.IO.Path.GetFullPath(path), System.UriKind.Absolute));
                            // Find the image element by name
                            var image = this.FindName("OdontogrammeImage") as Image;
                            if (image != null)
                            {
                                image.Source = bitmap;
                            }
                            System.Diagnostics.Debug.WriteLine($"Image loaded from: {path}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                }
            };
        }
    }
}
