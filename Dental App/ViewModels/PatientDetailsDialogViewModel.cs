using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Windows;
using Dental_App.Models;

namespace Dental_App.ViewModels
{
    public class PatientDetailsDialogViewModel : BindableBase
    {
        private string _title = "Dossier Patient";
        private string _buttonText = "Ajouter Paiement";
        private Patient _patient;
        private string _initials;
        private string _fullName;
        private decimal _totalAmount;
        private decimal _paidAmount;
        private decimal _remainingAmount;

        public PatientDetailsDialogViewModel(Patient patient = null)
        {
            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            if (patient != null)
            {
                InitializeWithPatient(patient);
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

        public Patient Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        public string Initials
        {
            get => _initials;
            set => SetProperty(ref _initials, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set => SetProperty(ref _paidAmount, value);
        }

        public decimal RemainingAmount
        {
            get => _remainingAmount;
            set => SetProperty(ref _remainingAmount, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public Action<bool?> CloseDialog { get; set; }

        private void InitializeWithPatient(Patient patient)
        {
            Patient = patient;
            FullName = $"{patient.Prenom} {patient.Nom}";
            
            // Generate initials (first letter of first name + first letter of last name)
            if (!string.IsNullOrEmpty(patient.Prenom) && !string.IsNullOrEmpty(patient.Nom))
            {
                Initials = $"{patient.Prenom[0]}{patient.Nom[0]}".ToUpper();
            }

            System.Diagnostics.Debug.WriteLine($"PatientDetailsDialogViewModel initialized with patient: {FullName}");
        }

        private void ExecuteSave()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PatientDetailsDialogViewModel: Add Payment executed for patient {Patient?.Id}");
                // TODO: Implement add payment logic
                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel() => CloseDialog?.Invoke(false);
    }
}
