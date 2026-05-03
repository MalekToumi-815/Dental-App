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

namespace Dental_App.ViewModels
{
    public class RadioImagesViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IRadioImageService _radioImageService;
        private readonly ILiveSearchService<Patient> _liveSearchService;

        private ObservableCollection<PatientDisplayItem> _patients;
        private ObservableCollection<RadioImageDisplayItem> _radioImages;
        private PatientDisplayItem _selectedPatient;
        private RadioImageDisplayItem _selectedImage;
        private bool _hasSelectedPatient;
        private bool _isLoading;
        private string _searchText = string.Empty;

        public RadioImagesViewModel(IPatientService patientService, IRadioImageService radioImageService, ILiveSearchService<Patient> liveSearchService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _radioImageService = radioImageService ?? throw new ArgumentNullException(nameof(radioImageService));
            _liveSearchService = liveSearchService ?? throw new ArgumentNullException(nameof(liveSearchService));

            SelectPatientCommand = new DelegateCommand<PatientDisplayItem>(SelectPatient);
            ViewImageCommand = new DelegateCommand<RadioImageDisplayItem>(ViewImage);
            CloseImageViewerCommand = new DelegateCommand(CloseImageViewer);
            AddRadioCommand = new DelegateCommand(AddRadio, CanAddRadio).ObservesProperty(() => HasSelectedPatient);

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

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                var patients = await _patientService.GetAllAsync();
                
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
                    RadioImages.Add(new RadioImageDisplayItem
                    {
                        Id = image.Id,
                        PatientId = image.PatientId,
                        FileName = image.FileName,
                        ImagePath = _radioImageService.GetFullPath(image.PatientId, image.FileName),
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

                var dialogViewModel = new AddRadioDialogViewModel();
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
                            RadioImages.Add(new RadioImageDisplayItem
                            {
                                Id = newRadio.Id,
                                PatientId = newRadio.PatientId,
                                FileName = newRadio.FileName,
                                ImagePath = _radioImageService.GetFullPath(newRadio.PatientId, newRadio.FileName),
                                ImageType = newRadio.Type,
                                DateTaken = newRadio.DatePrise ?? DateTime.Now
                            });

                            System.Diagnostics.Debug.WriteLine($"Radio image added successfully: {newRadio.FileName}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error importing radio image: {ex.Message}");
                            MessageBox.Show($"Erreur lors de l'ajout de la radio: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
