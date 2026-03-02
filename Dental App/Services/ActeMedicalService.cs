using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    internal class ActeMedicalService
    {
        private readonly DentalContext _context;

        public ActeMedicalService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ActeMedical> CreateAsync(ActeMedical acteMedical)
        {
            if (acteMedical == null) throw new ArgumentNullException(nameof(acteMedical));
            ValidateActeMedical(acteMedical);

            _context.ActeMedicals.Add(acteMedical);
            await _context.SaveChangesAsync();
            return acteMedical;
        }

        public async Task<ActeMedical?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.ActeMedicals
                .Include(a => a.IdConsuls)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<ActeMedical>> GetAllAsync()
        {
            return await _context.ActeMedicals
                .Include(a => a.IdConsuls)
                .ToListAsync();
        }

        public async Task<List<ActeMedical>> GetByLibelleAsync(string libelle)
        {
            if (string.IsNullOrWhiteSpace(libelle)) return new List<ActeMedical>();
            return await _context.ActeMedicals
                .Where(a => EF.Functions.Like(a.Libelle, "%" + libelle + "%"))
                .Include(a => a.IdConsuls)
                .ToListAsync();
        }

        public async Task<List<ActeMedical>> GetByPrixRangeAsync(decimal minPrix, decimal maxPrix)
        {
            if (minPrix < 0 || maxPrix < 0 || minPrix > maxPrix)
                throw new ArgumentException("Les prix doivent être positifs et minPrix <= maxPrix.");
            return await _context.ActeMedicals
                .Where(a => a.Prix >= minPrix && a.Prix <= maxPrix)
                .Include(a => a.IdConsuls)
                .ToListAsync();
        }

        public async Task<ActeMedical> UpdateAsync(ActeMedical acteMedical)
        {
            if (acteMedical == null) throw new ArgumentNullException(nameof(acteMedical));
            if (acteMedical.Id <= 0) throw new ArgumentException("L'ID de l'acte médical est invalide.", nameof(acteMedical.Id));

            ValidateActeMedical(acteMedical);

            var existing = await GetByIdAsync(acteMedical.Id);
            if (existing == null) throw new InvalidOperationException($"L'acte médical avec l'ID {acteMedical.Id} n'existe pas.");

            existing.Libelle = acteMedical.Libelle;
            existing.Prix = acteMedical.Prix;

            _context.ActeMedicals.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            if (id <= 0) return false;
            return await _context.ActeMedicals.AnyAsync(a => a.Id == id);
        }

        public async Task<int> CountAsync()
        {
            return await _context.ActeMedicals.CountAsync();
        }

        public async Task<decimal> GetAveragePriceAsync()
        {
            var count = await CountAsync();
            if (count == 0) return 0;
            
            var all = await _context.ActeMedicals.ToListAsync();
            return all.Count == 0 ? 0 : all.Average(a => a.Prix);
        }

        public async Task<decimal> GetTotalPriceAsync()
        {
            var all = await _context.ActeMedicals.ToListAsync();
            return all.Sum(a => a.Prix);
        }

        public async Task<ActeMedical?> GetByLibelleExactAsync(string libelle)
        {
            if (string.IsNullOrWhiteSpace(libelle)) return null;
            return await _context.ActeMedicals
                .Include(a => a.IdConsuls)
                .FirstOrDefaultAsync(a => a.Libelle == libelle);
        }

        private void ValidateActeMedical(ActeMedical acteMedical)
        {
            if (string.IsNullOrWhiteSpace(acteMedical.Libelle))
                throw new ArgumentException("Le libellé de l'acte médical est requis.", nameof(acteMedical.Libelle));
            if (acteMedical.Prix < 0)
                throw new ArgumentException("Le prix ne peut pas être négatif.", nameof(acteMedical.Prix));
        }
    }
}
