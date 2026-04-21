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

        public Action<bool> CloseDialog { get; set; }

        public OrdonnanceDialogViewModel(IOrdonnanceService ordonnanceService, int patientId, string patientName)
        {
            _ordonnanceService = ordonnanceService ?? throw new ArgumentNullException(nameof(ordonnanceService));
            _patientId = patientId;
            _patientName = patientName;
            _date = DateTime.Now;
            
            MedicamentItems = new ObservableCollection<MedicamentWrapper>();
            
            // Add initial empty medicament
            var initMed = new MedicamentWrapper();
            initMed.PropertyChanged += (s, e) => ValidateForm();
            MedicamentItems.Add(initMed);

            AddMedicamentCommand = new DelegateCommand(ExecuteAddMedicament);
            RemoveMedicamentCommand = new DelegateCommand<MedicamentWrapper>(ExecuteRemoveMedicament);
            SaveCommand = new DelegateCommand(ExecuteSave, CanSave).ObservesProperty(() => IsFormValid);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            MedicamentItems.CollectionChanged += (s, e) => ValidateForm();
        }

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