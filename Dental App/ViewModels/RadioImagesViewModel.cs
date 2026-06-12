using Dental_App.Models;
using Dental_App.Services;
using Dental_App.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dental_App.ViewModels;
using Dental_App.Views;

namespace Dental_App.ViewModels
{
    public class RadioImagesViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IRadioImageService _radioImageService;
        private readonly ILiveSearchService<Patient> _liveSearchService;
        private readonly IAppNotificationService _notificationService; // Add notification service

        private ObservableCollection<PatientDisplayItem> _patients;
        private ObservableCollection<RadioImageDisplayItem> _radioImages;
        private PatientDisplayItem _selectedPatient;
        private RadioImageDisplayItem _selectedImage;
        private bool _hasSelectedPatient;
        private bool _isLoading;
        private string _searchText = string.Empty;

        public RadioImagesViewModel(IPatientService patientService, IRadioImageService radioImageService, ILiveSearchService<Patient> liveSearchService, IAppNotificationService notificationService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _radioImageService = radioImageService ?? throw new ArgumentNullException(nameof(radioImageService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService)); // Initialize notification service

            SelectPatientCommand = new DelegateCommand<PatientDisplayItem>(SelectPatient);
            ViewImageCommand = new DelegateCommand<RadioImageDisplayItem>(ViewImage);
            CloseImageViewerCommand = new DelegateCommand(CloseImageViewer);
            AddRadioCommand = new DelegateCommand(AddRadio, CanAddRadio).ObservesProperty(() => HasSelectedPatient);
            DeleteImageCommand = new DelegateCommand<RadioImageDisplayItem>(DeleteImage);

            Patients = new ObservableCollection<PatientDisplayItem>();
            RadioImages = new ObservableCollection<RadioImageDisplayItem>();

            _ = LoadPatientsAsync();
        }

        public ObservableCollection<PatientDisplayItem> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        public ObservableCollection<RadioImageDisplayItem> RadioImages
        {
            get => _radioImages;
            set => SetProperty(ref _radioImages, value);
        }

        public PatientDisplayItem SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        public RadioImageDisplayItem SelectedImage
        {
            get => _selectedImage;
            set => SetProperty(ref _selectedImage, value);
        }

        public bool HasSelectedPatient
        {
            get => _hasSelectedPatient;
            set => SetProperty(ref _hasSelectedPatient, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = FilterPatientsAsync();
                }
            }
        }

        public DelegateCommand<PatientDisplayItem> SelectPatientCommand { get; }
        public DelegateCommand<RadioImageDisplayItem> ViewImageCommand { get; }
        public DelegateCommand CloseImageViewerCommand { get; }
        public DelegateCommand AddRadioCommand { get; }
        public DelegateCommand<RadioImageDisplayItem> DeleteImageCommand { get; }

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                // Limit initial load to first 10 patients to improve performance in the patient selector
                System.Collections.Generic.List<Patient> patients = null;
                try
                {
                    // Use paged fetch exposed on IPatientService (used elsewhere in the app)
                    patients = await _patientService.GetPatientsAsync(1, 10);
                }
                catch
                {
                    // Fall back to GetAllAsync if paged method isn't available for some reason
                    patients = await _patientService.GetAllAsync();
                }

                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(new PatientDisplayItem
                    {
                        Id = patient.Id,
                        FullName = $"{patient.Prenom} {patient.Nom}",
                        Phone = patient.Telephone,
                        Initials = GetInitials(patient.Prenom, patient.Nom)
                    });
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {Patients.Count} patients from database");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading patients: {ex.Message}");
                MessageBox.Show($"Error loading patients: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterPatientsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadPatientsAsync();
            }
            else
            {
                IsLoading = true;
                try
                {
                    var results = await _liveSearchService.SearchAsync(SearchText.Trim(), async (term) => 
                        await _patientService.SearchByNameAsync(term));

                    if (results != null)
                    {
                        Patients.Clear();
                        foreach (var patient in results)
                        {
                            Patients.Add(new PatientDisplayItem
                            {
                                Id = patient.Id,
                                FullName = $"{patient.Prenom} {patient.Nom}",
                                Phone = patient.Telephone,
                                Initials = GetInitials(patient.Prenom, patient.Nom)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error live search: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void SelectPatient(PatientDisplayItem patient)
        {
            if (patient == null)
                return;

            try
            {
                // Deselect all other patients
                foreach (var p in Patients)
                {
                    p.IsSelected = false;
                }

                System.Diagnostics.Debug.WriteLine($"Selected patient: {patient.FullName}");
                patient.IsSelected = true;
                SelectedPatient = patient;
                HasSelectedPatient = true;

                await LoadRadioImagesAsync(patient.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting patient: {ex.Message}");
                MessageBox.Show($"Error selecting patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRadioImagesAsync(int patientId)
        {
            try
            {
                IsLoading = true;
                RadioImages.Clear();

                var radioImages = await _radioImageService.GetPatientImagesAsync(patientId);

                foreach (var image in radioImages)
                {
                    var fullPath = _radioImageService.GetFullPath(image.PatientId, image.FileName);

                    ImageSource imgSource = null;
                    try
                    {
                        if (File.Exists(fullPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // load into memory and release file
                            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            imgSource = bitmap;
                        }
                    }
                    catch (Exception imgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load image into memory: {imgEx.Message}");
                        imgSource = null;
                    }

                    RadioImages.Add(new RadioImageDisplayItem
                    {
                        Id = image.Id,
                        PatientId = image.PatientId,
                        FileName = image.FileName,
                        ImagePath = fullPath,
                        ImageSource = imgSource,
                        ImageType = image.Type,
                        DateTaken = image.DatePrise ?? DateTime.Now
                    });
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {RadioImages.Count} radio images for patient {patientId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading radio images: {ex.Message}");
                MessageBox.Show($"Error loading radio images: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void DeleteImage(RadioImageDisplayItem radioImage)
        {
            if (radioImage == null)
                return;

            try
            {
                // Show custom confirmation dialog
                var vm = new ConfirmationDialogViewModel()
                {
                    Title = "Confirmer la suppression",
                    Message = "Voulez-vous vraiment supprimer cette radio ?"
                };

                bool? dialogResult = null;

                vm.CloseAction = (res) =>
                {
                    dialogResult = res;
                };

                var view = new ConfirmationDialogView { DataContext = vm };
                var win = new Window
                {
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    WindowStyle = WindowStyle.None,
                    Background = System.Windows.Media.Brushes.Transparent,
                    AllowsTransparency = true,
                    ResizeMode = ResizeMode.NoResize
                };

                // Show dialog modally
                win.ShowDialog();

                if (dialogResult != true)
                    return;

                IsLoading = true;

                await _radioImageService.DeleteImageAsync(radioImage.Id);

                // Remove from collection
                var toRemove = RadioImages.FirstOrDefault(r => r.Id == radioImage.Id);
                if (toRemove != null)
                {
                    RadioImages.Remove(toRemove);
                }

                _notificationService.ShowSuccess("Radio supprimée.", "Succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting radio image: {ex.Message}");
                _notificationService.ShowError($"Erreur lors de la suppression: {ex.Message}", "Erreur");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Public refresh entry point so the view can request a reload when it becomes visible.
        /// Reloads the patient selector and currently selected patient's radio images.
        /// </summary>
        public void Refresh()
        {
            System.Diagnostics.Debug.WriteLine("RadioImagesViewModel: Refresh requested");
            _ = LoadPatientsAsync();
            if (SelectedPatient != null)
            {
                _ = LoadRadioImagesAsync(SelectedPatient.Id);
            }
        }

        private void ViewImage(RadioImageDisplayItem radioImage)
        {
            if (radioImage == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Viewing image: {radioImage.FileName}");
                SelectedImage = radioImage;

                // Create the image viewer dialog
                var dialogViewModel = new ImageViewerDialogViewModel(this);
                var dialogView = new ImageViewerDialogView { DataContext = dialogViewModel };

                // Create a dialog window
                var window = new Window
                {
                    Content = dialogView,
                    Width = 750,
                    Height = 750,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.NoResize
                };

                // Set up the close dialog action
                dialogViewModel.CloseDialog = () =>
                {
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error viewing image: {ex.Message}");
                MessageBox.Show($"Error opening image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseImageViewer()
        {
            SelectedImage = null;
        }

        private bool CanAddRadio()
        {
            return HasSelectedPatient;
        }

        private void AddRadio()
        {
            if (SelectedPatient == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine("AddRadio called");

                var dialogViewModel = new AddRadioDialogViewModel(_notificationService); // Pass notification service
                var dialogView = new AddRadioDialogView { DataContext = dialogViewModel };

                // Create a dialog window
                var window = new Window
                {
                    Content = dialogView,
                    SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Padding = new Thickness(10)
                };

                dialogViewModel.CloseDialog = async (result) =>
                {
                    if (result != null)
                    {
                        try
                        {
                            IsLoading = true;
                            System.Diagnostics.Debug.WriteLine($"Importing radio image: {result.SelectedType}");

                            // Import the image using the service
                            var newRadio = await _radioImageService.ImportRadioImageAsync(
                                SelectedPatient.Id,
                                result.FilePath,
                                result.SelectedType,
                                result.SelectedDate);

                            // Add to the collection
                            // Load into memory using BitmapImage OnLoad and Freeze
                            var newPath = _radioImageService.GetFullPath(newRadio.PatientId, newRadio.FileName);
                            ImageSource newImgSrc = null;
                            try
                            {
                                if (File.Exists(newPath))
                                {
                                    var bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(newPath, UriKind.Absolute);
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                    bitmap.EndInit();
                                    bitmap.Freeze();
                                    newImgSrc = bitmap;
                                }
                            }
                            catch (Exception imgEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to load new image into memory: {imgEx.Message}");
                            }

                            RadioImages.Add(new RadioImageDisplayItem
                            {
                                Id = newRadio.Id,
                                PatientId = newRadio.PatientId,
                                FileName = newRadio.FileName,
                                ImagePath = newPath,
                                ImageSource = newImgSrc,
                                ImageType = newRadio.Type,
                                DateTaken = newRadio.DatePrise ?? DateTime.Now
                            });

                            
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error importing radio image: {ex.Message}");
                            _notificationService.ShowError($"Erreur lors de l'ajout de la radio: {ex.Message}", "Erreur"); // Notify error
                        }
                        finally
                        {
                            IsLoading = false;
                        }
                    }
                    window.Close();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddRadio: {ex.Message}");
                _notificationService.ShowError($"Erreur: {ex.Message}", "Erreur"); // Notify error
            }
        }

        // Helper to compute initials from first and last name
        private string GetInitials(string firstName, string lastName)
        {
            var first = string.IsNullOrWhiteSpace(firstName) ? "?" : firstName[0].ToString().ToUpper();
            var last = string.IsNullOrWhiteSpace(lastName) ? "?" : lastName[0].ToString().ToUpper();
            return $"{first}{last}";
        }
    
        // ViewModel for the Image Viewer Dialog
        public class ImageViewerDialogViewModel : BindableBase
        {
            private readonly RadioImagesViewModel _parentViewModel;
            public Action CloseDialog { get; set; }

            public ImageViewerDialogViewModel(RadioImagesViewModel parentViewModel)
            {
                _parentViewModel = parentViewModel;
                CloseImageViewerCommand = new DelegateCommand(CloseImageViewer);
            }

            public RadioImageDisplayItem SelectedImage => _parentViewModel.SelectedImage;

            public DelegateCommand CloseImageViewerCommand { get; }

            private void CloseImageViewer()
            {
                _parentViewModel.CloseImageViewerCommand.Execute();
                CloseDialog?.Invoke();
            }
        }

        public class PatientDisplayItem : BindableBase
        {
            private int _id;
            private string _fullName;
            private string _phone;
            private bool _isSelected;
            private string _initials;

            public int Id
            {
                get => _id;
                set => SetProperty(ref _id, value);
            }

            public string FullName
            {
                get => _fullName;
                set => SetProperty(ref _fullName, value);
            }

            public string Phone
            {
                get => _phone;
                set => SetProperty(ref _phone, value);
            }

            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }

            public string Initials
            {
                get => _initials;
                set => SetProperty(ref _initials, value);
            }
        }

        public class RadioImageDisplayItem : BindableBase
        {
            private int _id;
            private int _patientId;
            private string _fileName;
            private string _imagePath;
            private ImageSource _imageSource;
            private string _imageType;
            private DateTime _dateTaken;

            public int Id
            {
                get => _id;
                set => SetProperty(ref _id, value);
            }

            public int PatientId
            {
                get => _patientId;
                set => SetProperty(ref _patientId, value);
            }

            public string FileName
            {
                get => _fileName;
                set => SetProperty(ref _fileName, value);
            }

            public string ImagePath
            {
                get => _imagePath;
                set => SetProperty(ref _imagePath, value);
            }

            public ImageSource ImageSource
            {
                get => _imageSource;
                set => SetProperty(ref _imageSource, value);
            }

            public string ImageType
            {
                get => _imageType;
                set => SetProperty(ref _imageType, value);
            }

            public DateTime DateTaken
            {
                get => _dateTaken;
                set => SetProperty(ref _dateTaken, value);
            }
        }
    }
}
