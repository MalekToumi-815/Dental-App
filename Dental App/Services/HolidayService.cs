namespace Dental_App.Services
{
    /// <summary>
    /// Service pour gérer les jours fériés
    /// </summary>
    public interface IHolidayService
    {
        /// <summary>
        /// Récupère les jours fériés pour une année donnée
        /// </summary>
        List<DateTime> GetHolidaysForYear(int year);

        /// <summary>
        /// Vérifie si une date est un jour férié
        /// </summary>
        bool IsHoliday(DateTime date);

        /// <summary>
        /// Obtient le nom du jour férié
        /// </summary>
        string GetHolidayName(DateTime date);
    }

    /// <summary>
    /// Implémentation du service des jours fériés tunisiens
    /// </summary>
    public class HolidayService : IHolidayService
    {
        private readonly Dictionary<(int Month, int Day), string> _tunisianHolidays = new()
        {
            { (1, 1), "Nouvel An" },                   // 1er janvier
            { (3, 20), "Indépendance" },              // 20 mars
            { (4, 9), "Journée des Martyrs" },        // 9 avril
            { (5, 1), "Fête du Travail" },            // 1er mai
            { (7, 25), "Fête de la République" },     // 25 juillet
            { (8, 13), "Fête de la Femme" },          // 13 août
            { (10, 15), "Fête de l'Évacuation" }      // 15 octobre
        };

        public List<DateTime> GetHolidaysForYear(int year)
        {
            var holidays = new List<DateTime>();

            foreach (var ((month, day), name) in _tunisianHolidays)
            {
                var date = new DateTime(year, month, day);
                holidays.Add(date);
            }

            return holidays.OrderBy(d => d).ToList();
        }

        public bool IsHoliday(DateTime date)
        {
            var key = (date.Month, date.Day);
            return _tunisianHolidays.ContainsKey(key);
        }

        public string GetHolidayName(DateTime date)
        {
            var key = (date.Month, date.Day);
            return _tunisianHolidays.TryGetValue(key, out var name) ? name : string.Empty;
        }
    }
}
