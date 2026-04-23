using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Dental_App.Models;
using Dental_App.Services;

namespace Dental_App.ViewModels
{
    public class OrdonnanceDialogViewModel : BindableBase
    {
        private readonly IOrdonnanceService _ordonnanceService;
        private readonly int _patientId;
        private DateTime? _date;
        private string _patientName;
        private bool _isFormValid;
        private ObservableCollection<MedicamentWrapper> _medicamentItems;
        private ObservableCollection<MedicamentWrapper> _deletedMedicaments;
        private Ordonnance _ordonnanceBeingEdited;
        private bool _isEditMode;
        private bool _isReadOnlyMode;
        private string _title = "Nouvelle Ordonnance";

        public Action<bool> CloseDialog { get; set; }

        public OrdonnanceDialogViewModel(IOrdonnanceService ordonnanceService, int patientId, string patientName, Ordonnance ordonnanceToEdit = null)
        {
            _ordonnanceService = ordonnanceService ?? throw new ArgumentNullException(nameof(ordonnanceService));
            _patientId = patientId;
            _patientName = patientName;
            
            MedicamentItems = new ObservableCollection<MedicamentWrapper>();
            _deletedMedicaments = new ObservableCollection<MedicamentWrapper>();

            if (ordonnanceToEdit != null)
            {
                _isEditMode = true;
                _isReadOnlyMode = true;
                Title = "Détails Ordonnance";
                _ordonnanceBeingEdited = ordonnanceToEdit;
                _date = ordonnanceToEdit.DateCreation;
                
                if (ordonnanceToEdit.Medicaments?.Count > 0)
                {
                    foreach (var med in ordonnanceToEdit.Medicaments)
                    {
                        var medWrap = new MedicamentWrapper { Id = med.Id, Nom = med.Nom, Posologie = med.Posologie };
                        medWrap.PropertyChanged += (s, e) => ValidateForm();
                        MedicamentItems.Add(medWrap);
                    }
                }
                else
                {
                    var initMed = new MedicamentWrapper();
                    initMed.PropertyChanged += (s, e) => ValidateForm();
                    MedicamentItems.Add(initMed);
                }
            }
            else 
            {
                _isReadOnlyMode = false;
                _date = DateTime.Now;
                // Add initial empty medicament
                var initMed = new MedicamentWrapper();
                initMed.PropertyChanged += (s, e) => ValidateForm();
                MedicamentItems.Add(initMed);
            }

            AddMedicamentCommand = new DelegateCommand(ExecuteAddMedicament);
            RemoveMedicamentCommand = new DelegateCommand<MedicamentWrapper>(ExecuteRemoveMedicament);
            SaveCommand = new DelegateCommand(ExecuteSave, CanSave).ObservesProperty(() => IsFormValid);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            ToggleEditCommand = new DelegateCommand(ExecuteToggleEdit);

            MedicamentItems.CollectionChanged += (s, e) => ValidateForm();
            ValidateForm();
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsReadOnlyMode
        {
            get => _isReadOnlyMode;
            set 
            { 
                if (SetProperty(ref _isReadOnlyMode, value))
                {
                    RaisePropertyChanged(nameof(IsEditable));
                }
            }
        }

        public bool IsEditable => !IsReadOnlyMode;

        public bool ShowEditButton => _isEditMode && IsReadOnlyMode;

        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        public DateTime? Date
        {
            get => _date;
            set { if (SetProperty(ref _date, value)) ValidateForm(); }
        }

        public ObservableCollection<MedicamentWrapper> MedicamentItems
        {
            get => _medicamentItems;
            set => SetProperty(ref _medicamentItems, value);
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        public DelegateCommand AddMedicamentCommand { get; }
        public DelegateCommand<MedicamentWrapper> RemoveMedicamentCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ToggleEditCommand { get; }

        private void ExecuteToggleEdit()
        {
            IsReadOnlyMode = false;
            RaisePropertyChanged(nameof(ShowEditButton));
        }

        private void ExecuteAddMedicament()
        {
            var newMed = new MedicamentWrapper();
            newMed.PropertyChanged += (s, e) => ValidateForm();
            MedicamentItems.Add(newMed);
            ValidateForm();
        }

        private void ExecuteRemoveMedicament(MedicamentWrapper medicament)
        {
            if (MedicamentItems.Count > 1)
            {
                if (medicament.Id > 0)
                {
                    _deletedMedicaments.Add(medicament);
                }
                MedicamentItems.Remove(medicament);
                ValidateForm();
            }
        }

        private bool CanSave()
        {
            return IsFormValid;
        }

        private void ValidateForm()
        {
            // Basic validation
            IsFormValid = Date.HasValue && 
                          MedicamentItems.Any() &&
                          MedicamentItems.All(m => !string.IsNullOrWhiteSpace(m.Nom));
        }

        private async void ExecuteSave()
        {
            if (!IsFormValid) return;

            try
            {
                if (_isEditMode && _ordonnanceBeingEdited != null)
                {
                    // We create a detached DTO so we don't directly manipulate EF's tracked relationship here
                    var updateDto = new Ordonnance
                    {
                        Id = _ordonnanceBeingEdited.Id,
                        PatientId = _ordonnanceBeingEdited.PatientId,
                        DateCreation = Date ?? DateTime.Now,
                        Medicaments = MedicamentItems.Select(m => new Medicament 
                        {
                            Id = m.Id,
                            Nom = m.Nom,
                            Posologie = m.Posologie ?? string.Empty,
                            OrdonnanceId = _ordonnanceBeingEdited.Id
                        }).ToList()
                    };

                    await _ordonnanceService.UpdateAsync(updateDto);
                }
                else
                {
                    var ordonnance = new Ordonnance
                    {
                        PatientId = _patientId,
                        DateCreation = Date ?? DateTime.Now,
                        Medicaments = MedicamentItems.Select(m => new Medicament 
                        {
                            Nom = m.Nom,
                            Posologie = m.Posologie ?? string.Empty,
                        }).ToList()
                    };

                    await _ordonnanceService.CreateAsync(ordonnance);
                }

                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error Saving Ordonnance] {ex.Message}");
                // Handle error implicitly
            }
        }

        private void ExecuteCancel()
        {
            CloseDialog?.Invoke(false);
        }
    }

    /// <summary>
    /// Wrapper class to allow two-way binding on the individual medicament lines
    /// </summary>
    public class MedicamentWrapper : BindableBase
    {
        public int Id { get; set; } = 0;

        private string _nom = string.Empty;
        public string Nom
        {
            get => _nom;
            set => SetProperty(ref _nom, value);
        }

        private string _posologie = string.Empty;
        public string Posologie
        {
            get => _posologie;
            set => SetProperty(ref _posologie, value);
        }
    }
}