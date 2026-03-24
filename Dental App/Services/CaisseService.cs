using Dental_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dental_App.Services
{
    public interface ICaisseService
    {
        Task<Caisse> GetCaisseByDateAsync(DateOnly date);
        Task<List<Caisse>> GetAllCaisseAsync();
        Task<List<Caisse>> GetCaisseByDateRangeAsync(DateOnly startDate, DateOnly endDate);
        Task<bool> UpsertCaisseAsync(decimal montant);
        Task<bool> AddMontantAsync(decimal montant);
        Task<decimal> GetTotalMontantAsync(DateOnly startDate, DateOnly endDate);
        Task<Caisse> GetTodaysCaisseAsync();
    }

    public class CaisseService : ICaisseService
    {
        private readonly DentalContext _context;

        public CaisseService(DentalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get caisse entry for a specific date
        /// </summary>
        public async Task<Caisse> GetCaisseByDateAsync(DateOnly date)
        {
            return await Task.FromResult(_context.Caisses.FirstOrDefault(c => c.DateDuJour == date));
        }

        /// <summary>
        /// Get all caisse entries
        /// </summary>
        public async Task<List<Caisse>> GetAllCaisseAsync()
        {
            return await Task.FromResult(_context.Caisses.ToList());
        }

        /// <summary>
        /// Get caisse entries for a date range
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
        /// Add or update caisse entry for today
        /// </summary>
        public async Task<bool> UpsertCaisseAsync(decimal montant)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var caisse = _context.Caisses.FirstOrDefault(c => c.DateDuJour == today);

                if (caisse == null)
                {
                    caisse = new Caisse { DateDuJour = today, Montant = montant };
                    _context.Caisses.Add(caisse);
                }
                else
                {
                    caisse.Montant += montant;
                    _context.Caisses.Update(caisse);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error upserting caisse: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add amount to caisse for today
        /// </summary>
        public async Task<bool> AddMontantAsync(decimal montant)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var caisse = _context.Caisses.FirstOrDefault(c => c.DateDuJour == today);

                if (caisse == null)
                {
                    caisse = new Caisse { DateDuJour = today, Montant = montant };
                    _context.Caisses.Add(caisse);
                }
                else
                {
                    caisse.Montant = (caisse.Montant ?? 0) + montant;
                    _context.Caisses.Update(caisse);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding montant: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get total montant for a date range
        /// </summary>
        public async Task<decimal> GetTotalMontantAsync(DateOnly startDate, DateOnly endDate)
        {
            var total = await Task.FromResult(
                _context.Caisses
                    .Where(c => c.DateDuJour >= startDate && c.DateDuJour <= endDate)
                    .Sum(c => c.Montant ?? 0)
            );
            return total;
        }

        /// <summary>
        /// Get caisse entry for today
        /// </summary>
        public async Task<Caisse> GetTodaysCaisseAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await GetCaisseByDateAsync(today);
        }
    }
}
