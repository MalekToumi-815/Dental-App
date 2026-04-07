using Dental_App.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;

namespace Dental_App.ViewModels
{
    public class ActeDialogViewModel : BindableBase
    {
        private string _libelle = string.Empty;
        private string _title = string.Empty;
        private string _buttonText = "Ajouter";
        private DelegateCommand _saveCommand;
        private DelegateCommand? _cancelCommand;

        public string Libelle
        {
            get => _libelle;
            set
            {
                // SetProperty returns true if the value actually changed
                if (SetProperty(ref _libelle, value))
                {
                    // This tells the SaveCommand to re-evaluate CanSave()
                    _saveCommand?.RaiseCanExecuteChanged();
                }
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

        public DelegateCommand SaveCommand
        {
            get => _saveCommand;
        }

        public DelegateCommand CancelCommand
        {
            get => _cancelCommand ?? new DelegateCommand(ExecuteCancel);
        }

        public Action<ActeMedical?>? CloseDialog { get; set; }

        private ActeMedical? _acteBeingEdited;

        public ActeDialogViewModel(ActeMedical? acteToEdit = null)
        {
            // Ensure your command is initialized correctly FIRST before setting any properties
            _saveCommand = new DelegateCommand(ExecuteSave, CanSave);
            
            if (acteToEdit != null)
            {
                Title = "Modifier l'acte";
                Libelle = acteToEdit.Libelle;
                ButtonText = "Mettre a jour";
                _acteBeingEdited = acteToEdit;
            }
            else
            {
                Title = "Nouvel Acte Medical";
                Libelle = string.Empty;
                ButtonText = "Ajouter";
                _acteBeingEdited = null;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Libelle);
        }

        private void ExecuteSave()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Libelle))
                {
                    System.Windows.MessageBox.Show("Le libellé ne peut pas être vide", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                ActeMedical result;

                if (_acteBeingEdited != null)
                {
                    // Edit mode - update existing
                    _acteBeingEdited.Libelle = Libelle;
                    result = _acteBeingEdited;
                }
                else
                {
                    // Add mode - create new
                    result = new ActeMedical { Libelle = Libelle };
                }

                CloseDialog?.Invoke(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExecuteSave: {ex.Message}");
                System.Windows.MessageBox.Show($"Erreur: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            CloseDialog?.Invoke(null);
        }
    }
}
