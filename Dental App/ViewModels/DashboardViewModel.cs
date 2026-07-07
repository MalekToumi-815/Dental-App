using Dental_App.Models;
using Dental_App.Services;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Dental_App.ViewModels
{
    public class DashboardViewModel : BindableBase, INavigationAware
    {
        private readonly IPatientService _patientService;
        private readonly IRendezVousService _rvService;
        private readonly IConsultationService _consultService;
        private readonly ICaisseService _caisseService;
        private readonly IActeMedicalService _acteService;
        private readonly IOrdonnanceService _ordonnanceService;
        private readonly CultureInfo _frenchCulture = new CultureInfo("fr-FR");

        // Maximum visual height for the tallest bar in pixels
        private const double MaxBarVisualHeight = 240.0;
        // Default unit height per consult when counts are small
        private const double DefaultUnitHeight = 40.0;
        // Minimum bar height to keep visibility
        private const double MinBarHeight = 5.0;

        public DashboardViewModel(IPatientService p, IRendezVousService rv, IConsultationService c, ICaisseService cs, IActeMedicalService a, IOrdonnanceService o)
        {
            _patientService = p;
            _rvService = rv;
            _consultService = c;
            _caisseService = cs;
            _acteService = a;
            _ordonnanceService = o;

            TodayDate = DateTime.Now.ToString("dddd d MMMM yyyy", _frenchCulture);
            UpcomingAppointments = new ObservableCollection<AppointmentDisplayItem>();
            ConsultationChartData = new ObservableCollection<ChartBarItem>();

            // prepare month and year selections
            MonthNames = new ObservableCollection<string>(Enumerable.Range(1, 12).Select(i => CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.GetMonthName(i)));
            Years = new ObservableCollection<int>(Enumerable.Range(DateTime.Now.Year - 5, 6).Reverse());

            SelectedMonth = DateTime.Now.Month;
            SelectedMonthYear = DateTime.Now.Year;
            SelectedYearForAnnual = DateTime.Now.Year;

            // initialize editable text fields for year inputs
            SelectedMonthYearText = SelectedMonthYear.ToString();
            SelectedYearForAnnualText = SelectedYearForAnnual.ToString();

            LoadDashboardData();
        }

        public string TodayDate { get; }
        public ObservableCollection<AppointmentDisplayItem> UpcomingAppointments { get; }
        public ObservableCollection<ChartBarItem> ConsultationChartData { get; }

        public ObservableCollection<string> MonthNames { get; }
        public ObservableCollection<int> Years { get; }

        // Stat Properties
        private int _totalPatients; public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }
        private int _rvToday; public int RvToday { get => _rvToday; set => SetProperty(ref _rvToday, value); }
        private int _consultsMonth; public int ConsultsMonth { get => _consultsMonth; set => SetProperty(ref _consultsMonth, value); }

        // New stats replacing Actes / Ordonnances in dashboard
        private int _patientsPerYear; public int PatientsPerYear { get => _patientsPerYear; set => SetProperty(ref _patientsPerYear, value); }
        private int _patientsPerMonth; public int PatientsPerMonth { get => _patientsPerMonth; set => SetProperty(ref _patientsPerMonth, value); }

        private decimal _revenueToday; public decimal RevenueToday { get => _revenueToday; set => SetProperty(ref _revenueToday, value); }

        // Selection state
        private int _selectedMonth; public int SelectedMonth { get => _selectedMonth; set { if (SetProperty(ref _selectedMonth, value)) UpdatePatientsCountsAsync(); } }
        private int _selectedMonthYear; public int SelectedMonthYear { get => _selectedMonthYear; set { if (SetProperty(ref _selectedMonthYear, value)) { SelectedMonthYearText = value.ToString(); UpdatePatientsCountsAsync(); } } }
        private int _selectedYearForAnnual; public int SelectedYearForAnnual { get => _selectedYearForAnnual; set { if (SetProperty(ref _selectedYearForAnnual, value)) { SelectedYearForAnnualText = value.ToString(); UpdatePatientsCountsAsync(); } } }

        // Editable text bindings so user can type year manually
        private string _selectedMonthYearText;
        public string SelectedMonthYearText
        {
            get => _selectedMonthYearText;
            set
            {
                if (SetProperty(ref _selectedMonthYearText, value))
                {
                    // try parse typed value -> update int property when valid
                    if (int.TryParse(value, out var y) && y > 1900 && y < 3000)
                    {
                        SelectedMonthYear = y;
                    }
                }
            }
        }

        private string _selectedYearForAnnualText;
        public string SelectedYearForAnnualText
        {
            get => _selectedYearForAnnualText;
            set
            {
                if (SetProperty(ref _selectedYearForAnnualText, value))
                {
                    if (int.TryParse(value, out var y) && y > 1900 && y < 3000)
                    {
                        SelectedYearForAnnual = y;
                    }
                }
            }
        }

        // SelectedMonthIndex simplifies XAML binding (ComboBox SelectedIndex)
        private int _selectedMonthIndex; public int SelectedMonthIndex { get => _selectedMonthIndex; set { if (SetProperty(ref _selectedMonthIndex, value)) { SelectedMonth = value + 1; } } }

        private async void LoadDashboardData()
        {
            // 1. Fetch Stats
            TotalPatients = await _patientService.CountAsync();
            // compute patients per selected year/month
            await UpdatePatientsCountsAsync();

            var allRvs = await _rvService.GetAllAsync();
            RvToday = allRvs.Count(r => r.DateDebut.Date == DateTime.Today);

            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthly = await _consultService.GetByDateRangeAsync(startOfMonth, DateTime.Now);
            ConsultsMonth = monthly.Count;

            var (revenu, depense) = await _caisseService.GetTodaySummaryAsync();
            RevenueToday = revenu - depense;

            // 2. Build 7-Day Chart Data
            var last7DaysConsults = await _consultService.GetByDateRangeAsync(DateTime.Today.AddDays(-6), DateTime.Now);
            ConsultationChartData.Clear();

            // compute counts first
            var dayCounts = new int[7];
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                int count = last7DaysConsults.Count(c => c.DateConsultation.HasValue && c.DateConsultation.Value.Date == date.Date);
                dayCounts[6 - i] = count; // store in same order we will display
            }

            int maxCount = dayCounts.Max();

            // determine unit height: if maxCount * DefaultUnitHeight <= MaxBarVisualHeight then use DefaultUnitHeight
            // otherwise scale down so that maxCount maps to MaxBarVisualHeight
            double unitHeight = DefaultUnitHeight;
            if (maxCount > 0 && maxCount * DefaultUnitHeight > MaxBarVisualHeight)
            {
                unitHeight = MaxBarVisualHeight / maxCount;
            }

            // Now add items in display order (oldest to newest)
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(i - 6);
                int count = dayCounts[i];
                double barHeight = Math.Max(count * unitHeight, MinBarHeight);

                ConsultationChartData.Add(new ChartBarItem
                {
                    DayName = date.ToString("ddd", _frenchCulture),
                    Value = count,
                    BarHeight = barHeight
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

        private async System.Threading.Tasks.Task UpdatePatientsCountsAsync()
        {
            try
            {
                // Patients for selected month
                if (SelectedMonthYear <= 0) SelectedMonthYear = DateTime.Now.Year;
                if (SelectedMonth <= 0) SelectedMonth = DateTime.Now.Month;
                SelectedMonthIndex = SelectedMonth - 1;

                var monthStart = new DateTime(SelectedMonthYear, SelectedMonth, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                var monthConsults = await _consultService.GetByDateRangeAsync(monthStart, monthEnd);
                PatientsPerMonth = monthConsults.Select(c => c.PatientId).Distinct().Count();

                // Patients for selected year
                if (SelectedYearForAnnual <= 0) SelectedYearForAnnual = DateTime.Now.Year;
                var yearStart = new DateTime(SelectedYearForAnnual, 1, 1);
                var yearEnd = yearStart.AddYears(1).AddTicks(-1);
                var yearConsults = await _consultService.GetByDateRangeAsync(yearStart, yearEnd);
                PatientsPerYear = yearConsults.Select(c => c.PatientId).Distinct().Count();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du calcul des patients par période: {ex.Message}");
                PatientsPerMonth = 0;
                PatientsPerYear = 0;
            }
        }

        // INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Refresh data each time the dashboard is navigated to
            LoadDashboardData();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // Keep using the existing instance so OnNavigatedTo is called when returning
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // No-op
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