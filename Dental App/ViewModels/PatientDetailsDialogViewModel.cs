using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Windows;
using Dental_App.Models;
using Dental_App.Services;

namespace Dental_App.ViewModels
{
    public class PatientDetailsDialogViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IAppNotificationService _notificationService;
        private string _title = "Dossier Patient";
        private string _buttonText = "Ajouter Paiement";
        private Patient _patient;
        private string _initials;
        private string _fullName;
        private decimal _totalAmount;
        private decimal _paidAmount;
        private decimal _remainingAmount;
        private bool _isPaymentInputVisible;
        private string _paymentAmountInput = string.Empty;

        public PatientDetailsDialogViewModel(IPatientService patientService = null, IAppNotificationService notificationService = null, Patient patient = null)
        {
            _patientService = patientService;
            _notificationService = notificationService;
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

        public bool IsPaymentInputVisible
        {
            get => _isPaymentInputVisible;
            set => SetProperty(ref _isPaymentInputVisible, value);
        }

        public string PaymentAmountInput
        {
            get => _paymentAmountInput;
            set => SetProperty(ref _paymentAmountInput, value);
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

        private async void ExecuteSave()
        {
            try
            {
                // If payment input is visible, process the payment
                if (IsPaymentInputVisible)
                {
                    if (string.IsNullOrWhiteSpace(PaymentAmountInput))
                    {
                        _notificationService.ShowWarning("Veuillez entrer un montant");
                        return;
                    }

                    if (!decimal.TryParse(PaymentAmountInput, out decimal paymentAmount) || paymentAmount <= 0)
                    {
                        _notificationService.ShowWarning("Veuillez entrer un montant valide");
                        return;
                    }

                    if (_patientService == null || Patient == null)
                    {
                        _notificationService.ShowError("Erreur: Service ou patient non disponible");
                        return;
                    }

                    // Call the service to add the payment
                    var updatedPatient = await _patientService.AjouterMontantAsync(Patient.Id, paymentAmount);
                    
                    // Update the patient object
                    Patient = updatedPatient;
                    
                    // Update the paid amount and remaining amount
                    PaidAmount = updatedPatient.SommePaye ?? 0m;
                    RemainingAmount = TotalAmount - PaidAmount;

                    // Reset the input and hide the field
                    PaymentAmountInput = string.Empty;
                    IsPaymentInputVisible = false;
                    ButtonText = "Ajouter Paiement";

                    System.Diagnostics.Debug.WriteLine($"Payment of {paymentAmount} DT added for patient {Patient?.Id}. New paid amount: {PaidAmount}");
                    _notificationService.ShowSuccess("Le paiement a ete ajoute avec succes.");
                }
                else
                {
                    // Show the payment input field
                    IsPaymentInputVisible = true;
                    ButtonText = "Confirmer";
                    System.Diagnostics.Debug.WriteLine($"Payment input field opened for patient {Patient?.Id}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExecuteSave: {ex.Message}");
                _notificationService.ShowError($"Erreur: {ex.Message}");
            }
        }

        private void ExecuteCancel()
        {
            // If payment input is visible, hide it without saving
            if (IsPaymentInputVisible)
            {
                PaymentAmountInput = string.Empty;
                IsPaymentInputVisible = false;
                ButtonText = "Ajouter Paiement";
                System.Diagnostics.Debug.WriteLine("Payment input cancelled");
            }
            else
            {
                CloseDialog?.Invoke(false);
            }
        }
    }
}
