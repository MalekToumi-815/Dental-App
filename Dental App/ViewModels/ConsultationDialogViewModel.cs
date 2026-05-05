using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Dental_App.Models;
using Dental_App.Services;

namespace Dental_App.ViewModels
{
    public class ConsultationDialogViewModel : BindableBase
    {
        private readonly IConsultationService _consultationService;
        private readonly IDentService _dentService;
        private readonly IAppNotificationService _notificationService;
        private readonly int _patientId;
        private string _title = "Nouvelle Consultation";
        private string _buttonText = "Confirmer la Consultation";
        private string _subtitle = "";
        private DateTime? _date;
        private string _selectedTooth;
        private decimal _amount;
        private string _notes;
        private bool _isFormValid;
        private Consultation _consultationBeingEdited;
        private ObservableCollection<string> _toothOptions;

        public ConsultationDialogViewModel(IConsultationService consultationService, IDentService dentService, IAppNotificationService notificationService, int patientId, Consultation consultationToEdit = null)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _dentService = dentService ?? throw new ArgumentNullException(nameof(dentService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _patientId = patientId;

            // Initialize tooth options (FDI numbering)
            InitializeToothOptions();

            SaveCommand = new DelegateCommand(ExecuteSave, CanSave).ObservesProperty(() => IsFormValid);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // Initialize based on whether we're editing or adding
            if (consultationToEdit != null)
            {
                // Edit mode
                _consultationBeingEdited = consultationToEdit;
                Title = "Modifier Consultation";
                ButtonText = "Sauvegarder Consultation";

                Date = consultationToEdit.DateConsultation ?? DateTime.Now;
                SelectedTooth = consultationToEdit.IdDentNavigation?.CodeFdi.ToString() ?? string.Empty;
                Amount = consultationToEdit.MontantTotal ?? 0;
                Notes = consultationToEdit.Note ?? string.Empty;

                ValidateForm();
            }
            else
            {
                // Add mode
                Title = "Nouvelle Consultation";
                ButtonText = "Confirmer la Consultation";
                Date = DateTime.Now;
                Amount = 0;
                _consultationBeingEdited = null;
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

        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        public DateTime? Date
        {
            get => _date;
            set { if (SetProperty(ref _date, value)) ValidateForm(); }
        }

        public string SelectedTooth
        {
            get => _selectedTooth;
            set { if (SetProperty(ref _selectedTooth, value)) ValidateForm(); }
        }

        public decimal Amount
        {
            get => _amount;
            set { if (SetProperty(ref _amount, value)) ValidateForm(); }
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        public ObservableCollection<string> ToothOptions
        {
            get => _toothOptions;
            set => SetProperty(ref _toothOptions, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public Action<bool?> CloseDialog { get; set; }

        private void InitializeToothOptions()
        {
            var teeth = new ObservableCollection<string>();
            
            // FDI Numbering System
            // Adult teeth: 11-18 (upper right), 21-28 (upper left), 31-38 (lower left), 41-48 (lower right)
            for (int i = 11; i <= 18; i++) teeth.Add(i.ToString());
            for (int i = 21; i <= 28; i++) teeth.Add(i.ToString());
            for (int i = 31; i <= 38; i++) teeth.Add(i.ToString());
            for (int i = 41; i <= 48; i++) teeth.Add(i.ToString());
            // Child teeth: 51-55 (upper right), 61-65 (upper left), 71-75 (lower left), 81-85 (lower right)
            for (int i = 51; i <= 55; i++) teeth.Add(i.ToString());
            for (int i = 61; i <= 65; i++) teeth.Add(i.ToString());
            for (int i = 71; i <= 75; i++) teeth.Add(i.ToString());
            for (int i = 81; i <= 85; i++) teeth.Add(i.ToString());

            ToothOptions = teeth;
        }

        private void ValidateForm()
        {
            var isDateValid = Date.HasValue;
            var isToothValid = !string.IsNullOrWhiteSpace(SelectedTooth);
            var isAmountValid = Amount >= 0;

            IsFormValid = isDateValid && isToothValid && isAmountValid;

            System.Diagnostics.Debug.WriteLine($"ValidateForm: Date={isDateValid}, Tooth={isToothValid}, Amount={isAmountValid}, Final={IsFormValid}");
        }

        private bool CanSave() => IsFormValid;

        private async void ExecuteSave()
        {
            try
            {
                if (_consultationBeingEdited != null)
                {
                    // Edit mode - update existing consultation
                    _consultationBeingEdited.DateConsultation = Date;
                    _consultationBeingEdited.MontantTotal = Amount;
                    _consultationBeingEdited.Note = Notes;

                    // Set IdDent from selected tooth
                    if (!string.IsNullOrWhiteSpace(SelectedTooth) && int.TryParse(SelectedTooth, out int fdiCode))
                    {
                        var dent = await _dentService.GetDentByPatientAndFdiAsync(_patientId, fdiCode);
                        if (dent != null)
                        {
                            _consultationBeingEdited.IdDent = dent.Id;
                            System.Diagnostics.Debug.WriteLine($"Set IdDent to {dent.Id} for tooth FDI {fdiCode}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not find dent for patient {_patientId} with FDI code {fdiCode}");
                            _notificationService.ShowError($"Erreur: La dent {fdiCode} n'existe pas pour ce patient.");
                            return;
                        }
                    }

                    await _consultationService.UpdateAsync(_consultationBeingEdited);
                    System.Diagnostics.Debug.WriteLine($"Consultation {_consultationBeingEdited.Id} updated successfully");
                    _notificationService.ShowSuccess("La consultation a ete modifiee avec succes.", "Modification reussie");
                }
                else
                {
                    // Add mode - create new consultation
                    var newConsultation = new Consultation
                    {
                        PatientId = _patientId,
                        DateConsultation = Date,
                        MontantTotal = Amount,
                        Note = Notes
                    };

                    // Set IdDent from selected tooth
                    if (!string.IsNullOrWhiteSpace(SelectedTooth) && int.TryParse(SelectedTooth, out int fdiCode))
                    {
                        var dent = await _dentService.GetDentByPatientAndFdiAsync(_patientId, fdiCode);
                        if (dent != null)
                        {
                            newConsultation.IdDent = dent.Id;
                            System.Diagnostics.Debug.WriteLine($"Set IdDent to {dent.Id} for tooth FDI {fdiCode}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not find dent for patient {_patientId} with FDI code {fdiCode}");
                            _notificationService.ShowError($"Erreur: La dent {fdiCode} n'existe pas pour ce patient.");
                            return;
                        }
                    }

                    await _consultationService.CreateAsync(newConsultation);
                    System.Diagnostics.Debug.WriteLine("Consultation created successfully");
                    _notificationService.ShowSuccess("La consultation a ete creee avec succes.", "Ajout reussi");
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
