using System.Collections.ObjectModel;
using Prism.Mvvm;
using Dental_App.Services;

namespace Dental_App.ViewModels
{
    /// <summary>
    /// ViewModel pour l'affichage du calendrier annuel 2026
    /// </summary>
    public class AnnualCalendarViewModel : BindableBase
    {
        private readonly IHolidayService _holidayService;

        private const int CALENDAR_YEAR = 2026;

        private ObservableCollection<MonthCalendarViewModel> _months;
        public ObservableCollection<MonthCalendarViewModel> Months
        {
            get { return _months; }
            set { SetProperty(ref _months, value); }
        }

        private string _yearLabel = $"Calendrier 2026";
        public string YearLabel
        {
            get { return _yearLabel; }
            set { SetProperty(ref _yearLabel, value); }
        }

        public AnnualCalendarViewModel(IHolidayService holidayService)
        {
            _holidayService = holidayService ?? throw new ArgumentNullException(nameof(holidayService));
            Months = new ObservableCollection<MonthCalendarViewModel>();
            LoadCalendar();
        }

        private void LoadCalendar()
        {
            Months.Clear();

            for (int month = 1; month <= 12; month++)
            {
                var monthViewModel = new MonthCalendarViewModel(CALENDAR_YEAR, month, _holidayService);
                Months.Add(monthViewModel);
            }
        }
    }

    /// <summary>
    /// ViewModel pour un mois du calendrier
    /// </summary>
    public class MonthCalendarViewModel : BindableBase
    {
        private int _year;
        private int _month;
        private IHolidayService _holidayService;

        private string _monthName;
        public string MonthName
        {
            get { return _monthName; }
            set { SetProperty(ref _monthName, value); }
        }

        private ObservableCollection<DayViewModel> _days;
        public ObservableCollection<DayViewModel> Days
        {
            get { return _days; }
            set { SetProperty(ref _days, value); }
        }

        private ObservableCollection<string> _weekDayHeaders;
        public ObservableCollection<string> WeekDayHeaders
        {
            get { return _weekDayHeaders; }
            set { SetProperty(ref _weekDayHeaders, value); }
        }

        public MonthCalendarViewModel(int year, int month, IHolidayService holidayService)
        {
            _year = year;
            _month = month;
            _holidayService = holidayService;

            WeekDayHeaders = new ObservableCollection<string> { "Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam" };
            Days = new ObservableCollection<DayViewModel>();

            LoadMonth();
        }

        private void LoadMonth()
        {
            var culture = new System.Globalization.CultureInfo("fr-FR");
            var monthDate = new DateTime(_year, _month, 1);
            MonthName = monthDate.ToString("MMMM yyyy", culture);

            Days.Clear();

            // Récupérer le jour de la semaine du premier jour du mois (0 = Dimanche)
            var firstDayOfMonth = (int)monthDate.DayOfWeek;

            // Remplir les jours vides avant le premier jour du mois
            for (int i = 0; i < firstDayOfMonth; i++)
            {
                Days.Add(new DayViewModel { Day = 0, IsCurrentMonth = false });
            }

            // Remplir les jours du mois
            int daysInMonth = DateTime.DaysInMonth(_year, _month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(_year, _month, day);
                var dayViewModel = new DayViewModel
                {
                    Day = day,
                    IsCurrentMonth = true,
                    Date = date,
                    IsHoliday = _holidayService.IsHoliday(date),
                    IsWeekend = date.DayOfWeek == DayOfWeek.Sunday,
                    HolidayName = _holidayService.GetHolidayName(date)
                };

                Days.Add(dayViewModel);
            }

            // Remplir les jours vides après le dernier jour du mois (pour completer la grille 7x6)
            int remainingDays = (42 - Days.Count); // 6 semaines * 7 jours
            for (int i = 0; i < remainingDays; i++)
            {
                Days.Add(new DayViewModel { Day = 0, IsCurrentMonth = false });
            }
        }
    }

    /// <summary>
    /// ViewModel pour un jour du calendrier
    /// </summary>
    public class DayViewModel : BindableBase
    {
        private int _day;
        public int Day
        {
            get { return _day; }
            set { SetProperty(ref _day, value); }
        }

        private bool _isCurrentMonth;
        public bool IsCurrentMonth
        {
            get { return _isCurrentMonth; }
            set { SetProperty(ref _isCurrentMonth, value); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set { SetProperty(ref _date, value); }
        }

        private bool _isHoliday;
        public bool IsHoliday
        {
            get { return _isHoliday; }
            set { SetProperty(ref _isHoliday, value); }
        }

        private bool _isWeekend;
        public bool IsWeekend
        {
            get { return _isWeekend; }
            set { SetProperty(ref _isWeekend, value); }
        }

        private string _holidayName;
        public string HolidayName
        {
            get { return _holidayName; }
            set { SetProperty(ref _holidayName, value); }
        }

        public bool IsSpecialDay => IsHoliday || IsWeekend;
    }
}
