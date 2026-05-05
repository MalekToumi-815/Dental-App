using Dental_App.Models;
using Dental_App.Services;
using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class AddPatientDialogViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IAppNotificationService _notificationService;
        private string _title = "Nouveau Patient";
        private string _buttonText = "Ajouter";
        private string _nom;
        private string _prenom;
        private DateTime? _dateNaissance;
        private string _sexe;
        private string _telephone;
        private string _cin;
        private string _adresse;
        private string _profession;
        private bool _isFormValid;
        private Patient _patientBeingEdited;

        public AddPatientDialogViewModel(IPatientService patientService, IAppNotificationService notificationService, Patient patientToEdit = null)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            SexeOptions = new ObservableCollection<string> { "Masculin", "Feminin" };

            SaveCommand = new DelegateCommand(ExecuteSave, CanSave).ObservesProperty(() => IsFormValid);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // Initialize based on whether we're editing or adding
            if (patientToEdit != null)
            {
                // Edit mode
                _patientBeingEdited = patientToEdit;
                Title = "Modifier Patient";
                ButtonText = "Sauvegarder Patient";

                Nom = patientToEdit.Nom;
                Prenom = patientToEdit.Prenom;
                DateNaissance = patientToEdit.DateNaissance.ToDateTime(TimeOnly.MinValue);
                Sexe = patientToEdit.Sexe;
                Telephone = patientToEdit.Telephone;
                Cin = patientToEdit.Cin;
                Adresse = patientToEdit.Adresse;
                Profession = patientToEdit.Profession;

                ValidateForm();
            }
            else
            {
                // Add mode
                Title = "Ajouter un nouveau patient";
                ButtonText = "Ajouter";
                _patientBeingEdited = null;
            }
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
            set { if (SetProperty(ref _nom, value)) ValidateForm(); }
        }

        public string Prenom
        {
            get => _prenom;
            set { if (SetProperty(ref _prenom, value)) ValidateForm(); }
        }

        public DateTime? DateNaissance
        {
            get => _dateNaissance;
            set { if (SetProperty(ref _dateNaissance, value)) ValidateForm(); }
        }

        public string Sexe
        {
            get => _sexe;
            set { if (SetProperty(ref _sexe, value)) ValidateForm(); }
        }

        public string Telephone
        {
            get => _telephone;
            set { if (SetProperty(ref _telephone, value)) ValidateForm(); }
        }

        public string Cin
        {
            get => _cin;
            set => SetProperty(ref _cin, value);
        }

        public string Adresse
        {
            get => _adresse;
            set { if (SetProperty(ref _adresse, value)) ValidateForm(); }
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
            var isNomValid = !string.IsNullOrWhiteSpace(Nom);
            var isPrenomValid = !string.IsNullOrWhiteSpace(Prenom);
            var isDateValid = DateNaissance.HasValue;
            var isTelephoneValid = !string.IsNullOrWhiteSpace(Telephone);
            var isAdresseValid = !string.IsNullOrWhiteSpace(Adresse);

            IsFormValid = isNomValid && isPrenomValid && isDateValid && isTelephoneValid && isAdresseValid;

            System.Diagnostics.Debug.WriteLine($"ValidateForm: Nom={isNomValid}, Prenom={isPrenomValid}, Date={isDateValid}, Tel={isTelephoneValid}, Adresse={isAdresseValid}, Final={IsFormValid}");
            System.Diagnostics.Debug.WriteLine($"ValidateForm Values: Nom='{Nom}', Prenom='{Prenom}', Date={DateNaissance}, Tel='{Telephone}', Adresse='{Adresse}'");
        }

        private bool CanSave() => IsFormValid;

        private async void ExecuteSave()
        {
            try
            {
                if (_patientBeingEdited != null)
                {
                    // Edit mode - update existing patient
                    _patientBeingEdited.Nom = Nom;
                    _patientBeingEdited.Prenom = Prenom;
                    _patientBeingEdited.DateNaissance = DateOnly.FromDateTime(DateNaissance!.Value);
                    _patientBeingEdited.Sexe = Sexe;
                    _patientBeingEdited.Telephone = Telephone;
                    _patientBeingEdited.Cin = Cin;
                    _patientBeingEdited.Adresse = Adresse;
                    _patientBeingEdited.Profession = Profession;

                    await _patientService.UpdateAsync(_patientBeingEdited);
                    System.Diagnostics.Debug.WriteLine($"Patient {_patientBeingEdited.Id} updated successfully");
                    _notificationService.ShowSuccess("Le patient a ete modifie avec succes.", "Modification reussie");
                }
                else
                {
                    // Add mode - create new patient
                    var newPatient = new Patient
                    {
                        Nom = Nom,
                        Prenom = Prenom,
                        DateNaissance = DateOnly.FromDateTime(DateNaissance!.Value),
                        Sexe = Sexe,
                        Telephone = Telephone,
                        Cin = Cin,
                        Adresse = Adresse,
                        Profession = Profession,
                        SommePaye = 0m
                    };

                    await _patientService.CreateAsync(newPatient);
                    System.Diagnostics.Debug.WriteLine("Patient created successfully");
                    _notificationService.ShowSuccess("Le nouveau patient a ete ajoute avec succes.", "Ajout reussi");
                }

                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Erreur: {ex.Message}");
            }
        }

        private void ExecuteCancel() => CloseDialog?.Invoke(false);
    }
}