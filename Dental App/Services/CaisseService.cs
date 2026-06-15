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
        // Core operations
        Task<bool> AddOrUpdateCaisseAsync(decimal montant, bool isRevenu, DateOnly? date = null);
        // New: set (override) montant for a date/type
        Task<bool> SetCaisseAmountAsync(decimal montant, bool isRevenu, DateOnly? date = null);
        
        // Retrieval operations
        Task<List<Caisse>> GetAllCaisseAsync();
        Task<List<Caisse>> GetCaisseByDateRangeAsync(DateOnly startDate, DateOnly endDate);
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
        /// Add or update caisse entry for a specific date (or today if not specified)
        /// Each date can have maximum 2 entries: one Revenu (true) and one Dépense (false)
        /// If an entry already exists for this date and type, the montant is added to it
        /// </summary>
        public async Task<bool> AddOrUpdateCaisseAsync(decimal montant, bool isRevenu, DateOnly? date = null)
        {
            try
            {
                if (montant < 0)
                    throw new ArgumentException("Le montant ne peut pas être négatif.", nameof(montant));

                var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);
                
                // Find existing caisse entry for this date with the same type (Revenu or Dépense)
                var existingCaisse = _context.Caisses.FirstOrDefault(c => 
                    c.DateDuJour == targetDate && c.IsRevenu == isRevenu);

                if (existingCaisse == null)
                {
                    // Create new entry if it doesn't exist
                    var newCaisse = new Caisse
                    {
                        DateDuJour = targetDate,
                        Montant = montant,
                        IsRevenu = isRevenu
                    };
                    _context.Caisses.Add(newCaisse);
                }
                else
                {
                    // Add to existing montant
                    existingCaisse.Montant = (existingCaisse.Montant ?? 0) + montant;
                    _context.Caisses.Update(existingCaisse);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding or updating caisse: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set (override) the montant for a caisse entry for a specific date/type (or today if not specified)
        /// If an entry doesn't exist, it will be created with the provided montant.
        /// </summary>
        public async Task<bool> SetCaisseAmountAsync(decimal montant, bool isRevenu, DateOnly? date = null)
        {
            try
            {
                if (montant < 0)
                    throw new ArgumentException("Le montant ne peut pas être négatif.", nameof(montant));

                var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);

                var existingCaisse = _context.Caisses.FirstOrDefault(c =>
                    c.DateDuJour == targetDate && c.IsRevenu == isRevenu);

                if (existingCaisse == null)
                {
                    var newCaisse = new Caisse
                    {
                        DateDuJour = targetDate,
                        Montant = montant,
                        IsRevenu = isRevenu
                    };
                    _context.Caisses.Add(newCaisse);
                }
                else
                {
                    existingCaisse.Montant = montant;
                    _context.Caisses.Update(existingCaisse);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting caisse montant: {ex.Message}");
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
        /// Since design allows max 2 entries per date (1 Revenu, 1 Dépense), 
        /// this will typically return one of each or empty
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
