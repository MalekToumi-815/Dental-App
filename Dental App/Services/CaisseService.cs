using Dental_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dental_App.Services
{
    public interface ICaisseService
    {
        // Core operations
        Task<bool> AddOrUpdateCaisseAsync(decimal montant, bool isRevenu, DateOnly? date = null);
        
        // Retrieval operations
        Task<List<Caisse>> GetAllCaisseAsync();
        Task<List<Caisse>> GetCaisseByDateRangeAsync(DateOnly startDate, DateOnly endDate);
        Task<(decimal totalRevenu, decimal totalDepense)> GetDailySummaryAsync(DateOnly date);
        Task<(decimal totalRevenu, decimal totalDepense)> GetRangeSummaryAsync(DateOnly startDate, DateOnly endDate);
        
        // Today's operations
        Task<(decimal totalRevenu, decimal totalDepense)> GetTodaySummaryAsync();
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
        /// Get all caisse entries ordered by date
        /// </summary>
        public async Task<List<Caisse>> GetAllCaisseAsync()
        {
            return await Task.FromResult(
                _context.Caisses
                    .OrderByDescending(c => c.DateDuJour)
                    .ToList()
            );
        }

        /// <summary>
        /// Get caisse entries for a date range, ordered by date
        /// </summary>
        public async Task<List<Caisse>> GetCaisseByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await Task.FromResult(
                _context.Caisses
                    .Where(c => c.DateDuJour >= startDate && c.DateDuJour <= endDate)
                    .OrderBy(c => c.DateDuJour)
                    .ToList()
            );
        }

        /// <summary>
        /// Get daily summary: returns total Revenu and total Dépense for a specific date
        /// Since design allows max 2 entries per date (1 Revenu, 1 Dépense), 
        /// this will typically return one of each or empty
        /// </summary>
        public async Task<(decimal totalRevenu, decimal totalDepense)> GetDailySummaryAsync(DateOnly date)
        {
            var dailyCaisses = await Task.FromResult(
                _context.Caisses
                    .Where(c => c.DateDuJour == date)
                    .ToList()
            );

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
