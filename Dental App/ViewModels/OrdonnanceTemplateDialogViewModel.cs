using Dental_App.Services;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Dental_App.ViewModels
{
    public class OrdonnanceTemplateDialogViewModel : BindableBase
    {
        private readonly IOrdonnanceServiceTemplate _templateService;

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (SetProperty(ref _imagePath, value))
                {
                    RaisePropertyChanged(nameof(HasImage));
                    if (!string.IsNullOrEmpty(value) && File.Exists(value))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(value, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Prevents file locking
                            bitmap.EndInit();
                            bitmap.Freeze(); // Makes the image safe to use across threads
                            ImageSource = bitmap;
                            
                            // Capture the actual pixel dimensions of the image 
                            // to ensure our Canvas maps exactly to the real print coordinates 1:1
                            ImageWidth = bitmap.PixelWidth;
                            ImageHeight = bitmap.PixelHeight;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur lors du chargement de l'image : {ex.Message}");
                        }
                    }
                }
            }
        }

        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        public bool HasImage => !string.IsNullOrEmpty(ImagePath);

        private double _imageWidth = 800; // default fallback
        public double ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }

        private double _imageHeight = 1131; // default A4-ish fallback
        public double ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }

        private double _x;
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        private double _y;
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        private double _boxWidth = 400;
        public double BoxWidth
        {
            get => _boxWidth;
            set => SetProperty(ref _boxWidth, value);
        }

        private double _boxHeight = 300;
        public double BoxHeight
        {
            get => _boxHeight;
            set => SetProperty(ref _boxHeight, value);
        }

        public Action<bool> CloseDialog { get; set; }

        public DelegateCommand ImportImageCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public OrdonnanceTemplateDialogViewModel(IOrdonnanceServiceTemplate templateService)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

            ImportImageCommand = new DelegateCommand(ImportImage);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => HasImage).ObservesProperty(() => HasImage);
            CancelCommand = new DelegateCommand(() => CloseDialog?.Invoke(false));

            LoadCurrentSettings();
        }

        private async void LoadCurrentSettings()
        {
            try
            {
                var startingPoint = await _templateService.GetStartingCoordinatesAsync();
                X = startingPoint.X;
                Y = startingPoint.Y;

                var currentPath = await _templateService.GetTemplatePathAsync();
                
                // Construct full path if it's relative
                if (!string.IsNullOrEmpty(currentPath))
                {
                    string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string fullPath = Path.Combine(documents, "DentalApp_Data", currentPath);
                    if (File.Exists(fullPath))
                    {
                        ImagePath = fullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur au chargement des paramètres : {ex.Message}");
            }
        }

        private void ImportImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                Title = "Sélectionner une image d'en-tête"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
                
                // reset box position
                X = 50;
                Y = 50;
            }
        }

        private async Task SaveAsync()
        {
            if (!HasImage)
            {
                MessageBox.Show("Vous devez importer une image.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _templateService.SaveCoordinatesAsync(X, Y);
                await _templateService.SaveTemplateImageAsync(ImagePath);

                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
