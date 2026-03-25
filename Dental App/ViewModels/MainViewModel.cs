using Dental_App.Models;
using Dental_App.Services;
using Microsoft.EntityFrameworkCore;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IRendezVousService _rendezVousService;

        // Prism automatically injects these because they are registered!
        public MainViewModel(IPatientService patientService, IRendezVousService rendezVousService)
        {
            _patientService = patientService;
            _rendezVousService = rendezVousService;

            // Load initial data
            LoadPatientsCommand = new DelegateCommand(async () => await ExecuteLoadPatients());
        }

        private ObservableCollection<Patient> _patients;
        public ObservableCollection<Patient> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        public DelegateCommand LoadPatientsCommand { get; }

        private async Task ExecuteLoadPatients()
        {
            var list = await _patientService.GetAllAsync();
            Patients = new ObservableCollection<Patient>(list);
        }
    }
}