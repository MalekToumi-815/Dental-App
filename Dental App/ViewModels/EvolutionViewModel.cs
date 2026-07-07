using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dental_App.Services;
using System.Threading.Tasks;
using System.Globalization;

namespace Dental_App.ViewModels
{
    public class EvolutionViewModel : BindableBase
    {
        private int _year = DateTime.Now.Year;
        // Larger chart heights for better visibility
        private const double ChartMaxHeightMonthly = 300.0; // pixels for max monthly bar height
        private const double ChartMaxHeightYearly = 420.0;  // pixels for max yearly bar height
        private const double MinBarHeightMonthly = 6.0;     // minimum pixels for any non-zero monthly value
        private const double MinBarHeightYearly = 10.0;     // minimum pixels for any non-zero yearly value

        private readonly ICaisseService _caisseService;

        public EvolutionViewModel(ICaisseService caisseService)
        {
            _caisseService = caisseService ?? throw new ArgumentNullException(nameof(caisseService));

            MonthlyData = new ObservableCollection<MonthData>();
            YearlyData = new ObservableCollection<YearData>();

            // initialize months with localized month names
            for (int i = 1; i <= 12; i++)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                // Fallback to number if monthName is empty for some cultures
                if (string.IsNullOrWhiteSpace(monthName)) monthName = i.ToString();
                // Capitalize first letter according to current culture
                monthName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(monthName);
                MonthlyData.Add(new MonthData { Label = monthName });
            }

            // initialize years placeholders
            for (int i = 0; i < 4; i++)
            {
                YearlyData.Add(new YearData());
            }

            // Init UpdateCommand so UI can trigger reload
            UpdateCommand = new DelegateCommand(async () => await LoadYearAsync(Year));

            // Load data for the default year
            _ = LoadYearAsync(_year);
        }

        public int Year
        {
            get => _year;
            set
            {
                if (SetProperty(ref _year, value))
                {
                    // do not auto-load on each keystroke; user will click Mettre a jour
                    // but keep this here if you prefer auto-refresh on change
                    // _ = LoadYearAsync(_year);
                }
            }
        }

        public DelegateCommand UpdateCommand { get; }

        public ObservableCollection<MonthData> MonthlyData { get; }
        public ObservableCollection<YearData> YearlyData { get; }

        private async Task LoadYearAsync(int year)
        {
            try
            {
                // Load monthly data from DB
                for (int month = 1; month <= 12; month++)
                {
                    var start = new DateOnly(year, month, 1);
                    var end = start.AddMonths(1).AddDays(-1);

                    var list = await _caisseService.GetCaisseByDateRangeAsync(start, end);

                    decimal totalRev = list.Where(c => c.IsRevenu).Sum(c => c.Montant ?? 0m);
                    decimal totalExp = list.Where(c => !c.IsRevenu).Sum(c => c.Montant ?? 0m);
                    decimal net = totalRev - totalExp;

                    var idx = month - 1;
                    MonthlyData[idx].Revenue = totalRev;
                    MonthlyData[idx].Expense = totalExp;
                    MonthlyData[idx].Net = net;
                }

                // compute max for scaling (use absolute values to ensure tiny but non-zero show)
                decimal maxVal = MonthlyData.Max(m => new decimal[] { Math.Abs(m.Revenue), Math.Abs(m.Expense), Math.Abs(m.Net) }.Max());
                if (maxVal <= 0) maxVal = 1m;

                for (int i = 0; i < 12; i++)
                {
                    double revH = (double)(MonthlyData[i].Revenue / maxVal) * ChartMaxHeightMonthly;
                    double expH = (double)(MonthlyData[i].Expense / maxVal) * ChartMaxHeightMonthly;
                    double netH = (double)(Math.Max(0m, MonthlyData[i].Net) / maxVal) * ChartMaxHeightMonthly;

                    // enforce minimum visible height for any non-zero value
                    if (MonthlyData[i].Revenue > 0 && revH < MinBarHeightMonthly) revH = MinBarHeightMonthly;
                    if (MonthlyData[i].Expense > 0 && expH < MinBarHeightMonthly) expH = MinBarHeightMonthly;
                    if (MonthlyData[i].Net > 0 && netH < MinBarHeightMonthly) netH = MinBarHeightMonthly;

                    MonthlyData[i].RevenueHeight = revH;
                    MonthlyData[i].ExpenseHeight = expH;
                    MonthlyData[i].NetHeight = netH;
                }

                // yearly totals: previous 3 years and selected year
                var years = new[] { year - 3, year - 2, year - 1, year };
                YearlyData.Clear();

                foreach (var y in years)
                {
                    var startY = new DateOnly(y, 1, 1);
                    var endY = new DateOnly(y, 12, 31);
                    var listY = await _caisseService.GetCaisseByDateRangeAsync(startY, endY);
                    decimal totalRevY = listY.Where(c => c.IsRevenu).Sum(c => c.Montant ?? 0m);
                    decimal totalExpY = listY.Where(c => !c.IsRevenu).Sum(c => c.Montant ?? 0m);
                    decimal netY = totalRevY - totalExpY;

                    YearlyData.Add(new YearData
                    {
                        Year = y,
                        Revenue = totalRevY,
                        Expense = totalExpY,
                        Net = netY
                    });
                }

                decimal maxYear = Math.Max(1m, YearlyData.Max(y => new decimal[] { Math.Abs(y.Revenue), Math.Abs(y.Expense), Math.Abs(y.Net) }.Max()));
                foreach (var y in YearlyData)
                {
                    double rH = (double)(y.Revenue / maxYear) * ChartMaxHeightYearly;
                    double eH = (double)(y.Expense / maxYear) * ChartMaxHeightYearly;
                    double nH = (double)(Math.Max(0m, y.Net) / maxYear) * ChartMaxHeightYearly;

                    if (y.Revenue > 0 && rH < MinBarHeightYearly) rH = MinBarHeightYearly;
                    if (y.Expense > 0 && eH < MinBarHeightYearly) eH = MinBarHeightYearly;
                    if (y.Net > 0 && nH < MinBarHeightYearly) nH = MinBarHeightYearly;

                    y.RevenueHeight = rH;
                    y.ExpenseHeight = eH;
                    y.NetHeight = nH;
                }

                RaisePropertyChanged(nameof(MonthlyData));
                RaisePropertyChanged(nameof(YearlyData));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadYearAsync] Erreur: {ex.Message}");
            }
        }
    }

    public class MonthData : BindableBase
    {
        private string _label;
        private decimal _revenue;
        private decimal _expense;
        private decimal _net;
        private double _revHeight;
        private double _expHeight;
        private double _netHeight;

        public string Label { get => _label; set => SetProperty(ref _label, value); }
        public decimal Revenue { get => _revenue; set => SetProperty(ref _revenue, value); }
        public decimal Expense { get => _expense; set => SetProperty(ref _expense, value); }
        public decimal Net { get => _net; set => SetProperty(ref _net, value); }
        public double RevenueHeight { get => _revHeight; set => SetProperty(ref _revHeight, value); }
        public double ExpenseHeight { get => _expHeight; set => SetProperty(ref _expHeight, value); }
        public double NetHeight { get => _netHeight; set => SetProperty(ref _netHeight, value); }

        public MonthData()
        {
            Label = string.Empty;
        }
    }

    public class YearData : BindableBase
    {
        private int _year;
        private decimal _revenue;
        private decimal _expense;
        private decimal _net;
        private double _revHeight;
        private double _expHeight;
        private double _netHeight;

        public int Year { get => _year; set => SetProperty(ref _year, value); }
        public decimal Revenue { get => _revenue; set => SetProperty(ref _revenue, value); }
        public decimal Expense { get => _expense; set => SetProperty(ref _expense, value); }
        public decimal Net { get => _net; set => SetProperty(ref _net, value); }
        public double RevenueHeight { get => _revHeight; set => SetProperty(ref _revHeight, value); }
        public double ExpenseHeight { get => _expHeight; set => SetProperty(ref _expHeight, value); }
        public double NetHeight { get => _netHeight; set => SetProperty(ref _netHeight, value); }
    }
}
