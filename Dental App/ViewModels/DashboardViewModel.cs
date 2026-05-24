using Dental_App.Models;
using Dental_App.Services;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Dental_App.ViewModels
{
    public class DashboardViewModel : BindableBase
    {
        private readonly IPatientService _patientService;
        private readonly IRendezVousService _rvService;
        private readonly IConsultationService _consultService;
        private readonly ICaisseService _caisseService;
        private readonly IActeMedicalService _acteService;
        private readonly IOrdonnanceService _ordonnanceService;
        private readonly CultureInfo _frenchCulture = new CultureInfo("fr-FR");

        public DashboardViewModel(IPatientService p, IRendezVousService rv, IConsultationService c, ICaisseService cs, IActeMedicalService a, IOrdonnanceService o)
        {
            _patientService = p; _rvService = rv; _consultService = c; _caisseService = cs; _acteService = a; _ordonnanceService = o;

            TodayDate = DateTime.Now.ToString("dddd d MMMM yyyy", _frenchCulture);
            UpcomingAppointments = new ObservableCollection<AppointmentDisplayItem>();
            ConsultationChartData = new ObservableCollection<ChartBarItem>();
            LoadDashboardData();
        }

        public string TodayDate { get; }
        public ObservableCollection<AppointmentDisplayItem> UpcomingAppointments { get; }
        public ObservableCollection<ChartBarItem> ConsultationChartData { get; }

        // Stat Properties
        private int _totalPatients; public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }
        private int _rvToday; public int RvToday { get => _rvToday; set => SetProperty(ref _rvToday, value); }
        private int _consultsMonth; public int ConsultsMonth { get => _consultsMonth; set => SetProperty(ref _consultsMonth, value); }
        private int _actesTotal; public int ActesTotal { get => _actesTotal; set => SetProperty(ref _actesTotal, value); }
        private int _ordosActive; public int OrdosActive { get => _ordosActive; set => SetProperty(ref _ordosActive, value); }
        private decimal _revenueToday; public decimal RevenueToday { get => _revenueToday; set => SetProperty(ref _revenueToday, value); }

        private async void LoadDashboardData()
        {
            // 1. Fetch Stats
            TotalPatients = await _patientService.CountAsync();
            ActesTotal = await _acteService.CountAsync();
            OrdosActive = await _ordonnanceService.CountAsync();

            var allRvs = await _rvService.GetAllAsync();
            RvToday = allRvs.Count(r => r.DateDebut.Date == DateTime.Today);

            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthly = await _consultService.GetByDateRangeAsync(startOfMonth, DateTime.Now);
            ConsultsMonth = monthly.Count;

            var (revenu, depense) = await _caisseService.GetTodaySummaryAsync();
            RevenueToday = revenu-depense;

            // 2. Build 7-Day Chart Data
            var last7DaysConsults = await _consultService.GetByDateRangeAsync(DateTime.Today.AddDays(-6), DateTime.Now);
            ConsultationChartData.Clear();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                int count = last7DaysConsults.Count(c => c.DateConsultation.HasValue && c.DateConsultation.Value.Date == date.Date);
                ConsultationChartData.Add(new ChartBarItem
                {
                    DayName = date.ToString("ddd", _frenchCulture),
                    Value = count,
                    BarHeight = Math.Max(count * 40, 5) // Min height of 5 so the bar is visible even if small
                });
            }

            // 3. Upcoming List
            // Only today's appointments that are still upcoming (time >= now) and with status "en attente"
            var upcoming = allRvs
                .Where(r => r.DateDebut.Date == DateTime.Today && r.DateDebut >= DateTime.Now && r.Statut == "en attente")
                .OrderBy(r => r.DateDebut);
            UpcomingAppointments.Clear();
            foreach (var rv in upcoming)
            {
                var patient = await _patientService.GetByIdAsync(rv.PatientId);
                UpcomingAppointments.Add(new AppointmentDisplayItem
                {
                    PatientName = $"{patient?.Prenom} {patient?.Nom}",
                    Time = rv.DateDebut.ToString("HH:mm"),
                    Initials = $"{(patient?.Prenom?.FirstOrDefault())}{(patient?.Nom?.FirstOrDefault())}".ToUpper()
                });
            }
        }
    }

    public class ChartBarItem
    {
        public string DayName { get; set; }
        public int Value { get; set; }
        public double BarHeight { get; set; }
    }

    public class AppointmentDisplayItem
    {
        public string PatientName { get; set; }
        public string Time { get; set; }
        public string Initials { get; set; }
    }
}