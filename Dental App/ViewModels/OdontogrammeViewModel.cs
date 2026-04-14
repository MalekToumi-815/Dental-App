using Prism.Mvvm;
using Prism.Commands;
using System;

namespace Dental_App.ViewModels
{
    public class OdontogrammeViewModel : BindableBase
    {
        private string _patientInfo = "Aucun patient sélectionné";
        private DelegateCommand _choisirPatientCommand;

        public OdontogrammeViewModel()
        {
            ChoisirPatientCommand = new DelegateCommand(ExecuteChoisirPatient);
        }

        public string PatientInfo
        {
            get => _patientInfo;
            set => SetProperty(ref _patientInfo, value);
        }

        public DelegateCommand ChoisirPatientCommand
        {
            get => _choisirPatientCommand;
            set => SetProperty(ref _choisirPatientCommand, value);
        }

        private void ExecuteChoisirPatient()
        {
            System.Diagnostics.Debug.WriteLine("ExecuteChoisirPatient: Button clicked - Implementation pending");
        }
    }
}
