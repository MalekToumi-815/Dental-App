using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Windows;
using Dental_App.Models;
using Dental_App.Services;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Dental_App.ViewModels
{
    public class PatientDetailsDialogViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IAppNotificationService _notificationService;
        private readonly IRendezVousService _rendezVousService;
        private readonly IAntecedentService _antecedentService;
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
        private string _uniqueNumber;
        private ObservableCollection<string> _antecedents = new ObservableCollection<string>();
        private ObservableCollection<ActDisplayItem> _acts = new ObservableCollection<ActDisplayItem>();

        public PatientDetailsDialogViewModel(IPatientService patientService = null, IAppNotificationService notificationService = null, Patient patient = null, IRendezVousService rendezvousService = null, IAntecedentService antecedentService = null)
        {
            _patientService = patientService;
            _notificationService = notificationService;
            _rendezVousService = rendezvousService;
            _antecedentService = antecedentService;
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

        public string UniqueNumber
        {
            get => _uniqueNumber;
            private set => SetProperty(ref _uniqueNumber, value);
        }

        // New: Antecedents collection exposed to view
        public ObservableCollection<string> Antecedents
        {
            get => _antecedents;
            set => SetProperty(ref _antecedents, value);
        }

        // New: Acts collection for display (consultations + acts + tooth fdi)
        public ObservableCollection<ActDisplayItem> Acts
        {
            get => _acts;
            set => SetProperty(ref _acts, value);
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

            // Compute the unique number asynchronously
            ComputeUniqueNumberAsync(patient);

            // Load antecedents if service available
            _ = LoadAntecedentsAsync(patient.Id);

            // Load acts/consultations for this patient
            _ = LoadActsAsync(patient.Id);

            System.Diagnostics.Debug.WriteLine($"PatientDetailsDialogViewModel initialized with patient: {FullName}");
        }

        private async Task LoadAntecedentsAsync(int patientId)
        {
            try
            {
                Antecedents.Clear();
                if (_antecedentService == null) return;

                var list = await _antecedentService.GetByPatientIdAsync(patientId);
                if (list != null)
                {
                    foreach (var a in list)
                    {
                        Antecedents.Add(string.IsNullOrWhiteSpace(a.Nom) ? a.Description ?? "(sans titre)" : a.Nom + (string.IsNullOrWhiteSpace(a.Description) ? string.Empty : $": {a.Description}"));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAntecedentsAsync] Erreur: {ex.Message}");
            }
        }

        // New: load consultations and acts for patient
        private async Task LoadActsAsync(int patientId)
        {
            try
            {
                Acts.Clear();
                if (_patientService == null) return;

                var patientWithConsultations = await _patientService.GetByIdWithConsultationsAsync(patientId);
                if (patientWithConsultations?.Consultations == null) return;

                var consultations = patientWithConsultations.Consultations
                    .OrderByDescending(c => c.DateConsultation)
                    .ToList();

                foreach (var c in consultations)
                {
                    var actes = c.IdActes?.Select(a => a.Libelle).ToList() ?? new System.Collections.Generic.List<string>();
                    int? fdi = c.IdDentNavigation?.CodeFdi;
                    Acts.Add(new ActDisplayItem
                    {
                        ConsultationId = c.Id,
                        Date = c.DateConsultation ?? DateTime.Now,
                        DateFormatted = (c.DateConsultation ?? DateTime.Now).ToString("dd/MM/yyyy"),
                        ToothFdi = fdi,
                        ActNames = actes,
                        Notes = c.Note
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadActsAsync] Erreur: {ex.Message}");
            }
        }

        private async void ComputeUniqueNumberAsync(Patient patient)
        {
            try
            {
                int year = DateTime.Now.Year;

                if (_rendezVousService != null)
                {
                    var rvs = await _rendezVousService.GetByPatientIdAsync(patient.Id);
                    if (rvs != null && rvs.Count > 0)
                    {
                        var first = rvs.OrderBy(r => r.DateDebut).FirstOrDefault();
                        if (first != null)
                        {
                            year = first.DateDebut.Year;
                        }
                    }
                }

                UniqueNumber = $"{patient.Id}/{year}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComputeUniqueNumberAsync] Erreur: {ex.Message}");
                UniqueNumber = $"{patient.Id}/{DateTime.Now.Year}";
            }
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

    public class ActDisplayItem
    {
        public int ConsultationId { get; set; }
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; } = "";
        public int? ToothFdi { get; set; }
        public List<string> ActNames { get; set; } = new List<string>();
        public string Notes { get; set; } = "";
    }
}
