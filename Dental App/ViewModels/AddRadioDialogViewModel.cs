using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;

namespace Dental_App.ViewModels
{
    public class AddRadioDialogViewModel : BindableBase
    {
        private DateTime? _selectedDate;
        private string _selectedType;
        private string _selectedFilePath;
        private string _selectedFileName;
        private bool _isFormValid;

        public AddRadioDialogViewModel()
        {
            SelectedDate = DateTime.Now;

            BrowseFileCommand = new DelegateCommand(BrowseFile);
            SaveCommand = new DelegateCommand(Save, CanSave).ObservesProperty(() => IsFormValid);
            CancelCommand = new DelegateCommand(Cancel);
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    ValidateForm();
                }
            }
        }

        public string SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    ValidateForm();
                }
            }
        }

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        public string SelectedFileName
        {
            get => _selectedFileName;
            set => SetProperty(ref _selectedFileName, value);
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        public DelegateCommand BrowseFileCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action<RadioDialogResult> CloseDialog { get; set; }

        private void BrowseFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner une image radiographique",
                    Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp, *.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedFilePath = openFileDialog.FileName;
                    SelectedFileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    ValidateForm();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error browsing file: {ex.Message}");
                MessageBox.Show($"Erreur lors de la sélection du fichier: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateForm()
        {
            IsFormValid = SelectedDate.HasValue && 
                         !string.IsNullOrWhiteSpace(SelectedType) && 
                         !string.IsNullOrWhiteSpace(SelectedFilePath);
        }

        private bool CanSave()
        {
            return IsFormValid;
        }

        private void Save()
        {
            try
            {
                var result = new RadioDialogResult
                {
                    SelectedDate = SelectedDate.Value,
                    SelectedType = SelectedType?.Trim(),
                    FilePath = SelectedFilePath
                };

                CloseDialog?.Invoke(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ajout de la radio: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            CloseDialog?.Invoke(null);
        }
    }

    public class RadioDialogResult
    {
        public DateTime SelectedDate { get; set; }
        public string SelectedType { get; set; }
        public string FilePath { get; set; }
    }
}
