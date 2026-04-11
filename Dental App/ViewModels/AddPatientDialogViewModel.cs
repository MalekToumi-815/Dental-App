using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class AddPatientDialogViewModel : BindableBase
    {
        private string _title = "Nouveau Patient";
        private string _buttonText = "Ajouter";
        private string _nom;
        private string _prenom;
        private DateOnly? _dateNaissance;
        private string _sexe;
        private string _telephone;
        private string _cin;
        private string _adresse;
        private string _profession;
        private bool _isFormValid;

        public AddPatientDialogViewModel()
        {
            SexeOptions = new ObservableCollection<string> { "Masculin", "Féminin" };
            
            SaveCommand = new DelegateCommand(Save, CanSave).ObservesProperty(() => IsFormValid);
            CancelCommand = new DelegateCommand(Cancel);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string ButtonText
        {
            get => _buttonText;
            set => SetProperty(ref _buttonText, value);
        }

        public string Nom
        {
            get => _nom;
            set
            {
                if (SetProperty(ref _nom, value))
                {
                    ValidateForm();
                }
            }
        }

        public string Prenom
        {
            get => _prenom;
            set
            {
                if (SetProperty(ref _prenom, value))
                {
                    ValidateForm();
                }
            }
        }

        public DateOnly? DateNaissance
        {
            get => _dateNaissance;
            set
            {
                if (SetProperty(ref _dateNaissance, value))
                {
                    ValidateForm();
                }
            }
        }

        public string Sexe
        {
            get => _sexe;
            set => SetProperty(ref _sexe, value);
        }

        public string Telephone
        {
            get => _telephone;
            set
            {
                if (SetProperty(ref _telephone, value))
                {
                    ValidateForm();
                }
            }
        }

        public string Cin
        {
            get => _cin;
            set => SetProperty(ref _cin, value);
        }

        public string Adresse
        {
            get => _adresse;
            set
            {
                if (SetProperty(ref _adresse, value))
                {
                    ValidateForm();
                }
            }
        }

        public string Profession
        {
            get => _profession;
            set => SetProperty(ref _profession, value);
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        public ObservableCollection<string> SexeOptions { get; }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action<bool?> CloseDialog { get; set; }

        private void ValidateForm()
        {
            IsFormValid = !string.IsNullOrWhiteSpace(Nom) &&
                         !string.IsNullOrWhiteSpace(Prenom) &&
                         DateNaissance.HasValue &&
                         !string.IsNullOrWhiteSpace(Telephone) &&
                         !string.IsNullOrWhiteSpace(Adresse);
        }

        private bool CanSave()
        {
            return IsFormValid;
        }

        private void Save()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AddPatientDialogViewModel: Save called - {Prenom} {Nom}");
                // TODO: Call patient service to save the patient
                // For now, just close the dialog with success
                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving patient: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ajout du patient: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            CloseDialog?.Invoke(false);
        }
    }
}
