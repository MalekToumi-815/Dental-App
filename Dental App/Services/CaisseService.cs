using Dental_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    public interface ICaisseService
    {
        // Create / Update
        Task<bool> CreateCaisseAsync(decimal montant, bool isRevenu, string nom, DateOnly? date = null);
        Task<bool> UpdateCaisseAsync(int id, decimal montant, bool isRevenu, string nom, DateOnly date);

        // Retrieval operations
        Task<List<Caisse>> GetAllCaisseAsync();
        Task<List<Caisse>> GetCaisseByDateRangeAsync(DateOnly startDate, DateOnly endDate);
        Task<List<Caisse>> GetCaisseByNomAsync(string nom);
        Task<(decimal totalRevenu, decimal totalDepense)> GetDailySummaryAsync(DateOnly date);
        Task<(decimal totalRevenu, decimal totalDepense)> GetRangeSummaryAsync(DateOnly startDate, DateOnly endDate);

        // Today's operations
        Task<(decimal totalRevenu, decimal totalDepense)> GetTodaySummaryAsync();

        // Pagination support
        Task<List<Caisse>> GetCaisseAsync(int pageIndex, int pageSize = 10);
        Task<int> GetCaisseCountAsync();
    }

    public class CaisseService : ICaisseService
    {
        private readonly DentalContext _context;

        public CaisseService(DentalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new caisse entry. Each transaction is now its own independent row,
        /// identified by its Nom - multiple entries per date/type are allowed.
        /// </summary>
        public async Task<bool> CreateCaisseAsync(decimal montant, bool isRevenu, string nom, DateOnly? date = null)
        {
            try
            {
                if (montant < 0)
                    throw new ArgumentException("Le montant ne peut pas être négatif.", nameof(montant));

                var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);

                var newCaisse = new Caisse
                {
                    DateDuJour = targetDate,
                    Montant = montant,
                    IsRevenu = isRevenu,
                    Nom = nom ?? string.Empty
                };

                _context.Caisses.Add(newCaisse);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating caisse: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing caisse entry identified by its Id.
        /// </summary>
        public async Task<bool> UpdateCaisseAsync(int id, decimal montant, bool isRevenu, string nom, DateOnly date)
        {
            try
            {
                if (montant < 0)
                    throw new ArgumentException("Le montant ne peut pas être négatif.", nameof(montant));

                var existingCaisse = await _context.Caisses.FirstOrDefaultAsync(c => c.Id == id);

                if (existingCaisse == null)
                    return false;

                existingCaisse.DateDuJour = date;
                existingCaisse.Montant = montant;
                existingCaisse.IsRevenu = isRevenu;
                existingCaisse.Nom = nom ?? string.Empty;

                _context.Caisses.Update(existingCaisse);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating caisse: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all caisse entries ordered by date
        /// </summary>
        public async Task<List<Caisse>> GetAllCaisseAsync()
        {
            return await _context.Caisses
                    .OrderByDescending(c => c.DateDuJour)
                    .ToListAsync();
        }

        /// <summary>
        /// Get caisse entries for a date range, ordered by date
        /// </summary>
        public async Task<List<Caisse>> GetCaisseByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await _context.Caisses
                    .Where(c => c.DateDuJour >= startDate && c.DateDuJour <= endDate)
                    .OrderBy(c => c.DateDuJour)
                    .ToListAsync();
        }

        /// <summary>
        /// Search caisse entries by Nom (case-insensitive partial match), ordered by date (descending)
        /// </summary>
        public async Task<List<Caisse>> GetCaisseByNomAsync(string nom)
        {
            if (string.IsNullOrWhiteSpace(nom))
                return new List<Caisse>();

            return await _context.Caisses
                    .Where(c => EF.Functions.Like(c.Nom, $"%{nom}%"))
                    .OrderByDescending(c => c.DateDuJour)
                    .ToListAsync();
        }

        /// <summary>
        /// Get paginated caisse entries ordered by date (descending)
        /// pageIndex is 1-based. If pageIndex &lt;= 0 it will be treated as 1.
        /// </summary>
        public async Task<List<Caisse>> GetCaisseAsync(int pageIndex, int pageSize = 10)
        {
            if (pageSize <= 0) pageSize = 10;
            if (pageIndex <= 0) pageIndex = 1;

            return await _context.Caisses
                .OrderByDescending(c => c.DateDuJour)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Returns total number of caisse records in DB
        /// </summary>
        public async Task<int> GetCaisseCountAsync()
        {
            return await _context.Caisses.CountAsync();
        }

        /// <summary>
        /// Get daily summary: returns total Revenu and total Dépense for a specific date
        /// (sums across all named entries for that day)
        /// </summary>
        public async Task<(decimal totalRevenu, decimal totalDepense)> GetDailySummaryAsync(DateOnly date)
        {
            var dailyCaisses = await _context.Caisses
                    .Where(c => c.DateDuJour == date)
                    .ToListAsync();

            var totalRevenu = dailyCaisses
                .Where(c => c.IsRevenu)
                .Sum(c => c.Montant ?? 0);

            var totalDepense = dailyCaisses
                .Where(c => !c.IsRevenu)
                .Sum(c => c.Montant ?? 0);

            return (totalRevenu, totalDepense);
        }

        /// <summary>
        /// Get summary for a date range: returns total Revenu and total Dépense
        /// </summary>
        public async Task<(decimal totalRevenu, decimal totalDepense)> GetRangeSummaryAsync(DateOnly startDate, DateOnly endDate)
        {
            var rangedCaisses = await GetCaisseByDateRangeAsync(startDate, endDate);

            var totalRevenu = rangedCaisses
                .Where(c => c.IsRevenu)
                .Sum(c => c.Montant ?? 0);

            var totalDepense = rangedCaisses
                .Where(c => !c.IsRevenu)
                .Sum(c => c.Montant ?? 0);

            return (totalRevenu, totalDepense);
        }

        /// <summary>
        /// Get today's summary: returns total Revenu and total Dépense for today
        /// </summary>
        public async Task<(decimal totalRevenu, decimal totalDepense)> GetTodaySummaryAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await GetDailySummaryAsync(today);
        }
    }
}