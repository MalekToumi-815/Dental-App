using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    public interface IActeMedicalService
    {
        Task<ActeMedical> CreateAsync(ActeMedical acteMedical);
        Task<ActeMedical?> GetByIdAsync(int id);
        Task<List<ActeMedical>> GetAllAsync();
        Task<List<ActeMedical>> GetByLibelleAsync(string libelle);
        Task<ActeMedical> UpdateAsync(ActeMedical acteMedical);
        Task<bool> ExistsAsync(int id);
        Task<int> CountAsync();
        Task<ActeMedical?> GetByLibelleExactAsync(string libelle);

        // Deletion
        Task<bool> DeleteActeMedicalAsync(int id);
    }

    public class ActeMedicalService : IActeMedicalService
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
                .Where(a => EF.Functions.Like(a.Libelle,  libelle + "%"))
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

        public async Task<ActeMedical?> GetByLibelleExactAsync(string libelle)
        {
            if (string.IsNullOrWhiteSpace(libelle)) return null;
            return await _context.ActeMedicals
                .Include(a => a.IdConsuls)
                .FirstOrDefaultAsync(a => a.Libelle == libelle);
        }

        public async Task<bool> DeleteActeMedicalAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));

            var acte = await _context.ActeMedicals
                .Include(a => a.IdConsuls)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (acte == null) return false;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Clear many-to-many relationships (ActeConsultation)
                foreach (var consult in acte.IdConsuls.ToList())
                {
                    consult.IdActes.Remove(acte);
                }

                _context.ActeMedicals.Remove(acte);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting ActeMedical {id}: {ex.Message}");
                try { await tx.RollbackAsync(); } catch { }
                return false;
            }
        }

        private void ValidateActeMedical(ActeMedical acteMedical)
        {
            if (string.IsNullOrWhiteSpace(acteMedical.Libelle))
                throw new ArgumentException("Le libellé de l'acte médical est requis.", nameof(acteMedical.Libelle));
        }
    }
}
