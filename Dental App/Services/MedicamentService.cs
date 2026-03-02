using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    internal class MedicamentService
    {
        private readonly DentalContext _context;

        public MedicamentService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Medicament> CreateAsync(Medicament medicament)
        {
            if (medicament == null) throw new ArgumentNullException(nameof(medicament));
            ValidateMedicament(medicament);

            _context.Medicaments.Add(medicament);
            await _context.SaveChangesAsync();
            return medicament;
        }

        public async Task<Medicament?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.Medicaments
                .Include(m => m.Ordonnance)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<Medicament>> GetAllAsync()
        {
            return await _context.Medicaments
                .Include(m => m.Ordonnance)
                .ToListAsync();
        }

        public async Task<List<Medicament>> GetByOrdonnanceIdAsync(int ordonnanceId)
        {
            if (ordonnanceId <= 0) throw new ArgumentException("L'ID de l'ordonnance doit être supérieur à 0.", nameof(ordonnanceId));
            return await _context.Medicaments
                .Where(m => m.OrdonnanceId == ordonnanceId)
                .Include(m => m.Ordonnance)
                .ToListAsync();
        }

        public async Task<List<Medicament>> GetByNameAsync(string nom)
        {
            if (string.IsNullOrWhiteSpace(nom)) return new List<Medicament>();
            return await _context.Medicaments
                .Where(m => EF.Functions.Like(m.Nom, "%" + nom + "%"))
                .Include(m => m.Ordonnance)
                .ToListAsync();
        }

        public async Task<Medicament> UpdateAsync(Medicament medicament)
        {
            if (medicament == null) throw new ArgumentNullException(nameof(medicament));
            if (medicament.Id <= 0) throw new ArgumentException("L'ID du médicament est invalide.", nameof(medicament.Id));

            ValidateMedicament(medicament);

            var existing = await GetByIdAsync(medicament.Id);
            if (existing == null) throw new InvalidOperationException($"Le médicament avec l'ID {medicament.Id} n'existe pas.");

            existing.Nom = medicament.Nom;
            existing.Posologie = medicament.Posologie;
            existing.OrdonnanceId = medicament.OrdonnanceId;

            _context.Medicaments.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }


        public async Task<bool> ExistsAsync(int id)
        {
            if (id <= 0) return false;
            return await _context.Medicaments.AnyAsync(m => m.Id == id);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Medicaments.CountAsync();
        }

        public async Task<int> CountByOrdonnanceIdAsync(int ordonnanceId)
        {
            if (ordonnanceId <= 0) return 0;
            return await _context.Medicaments.CountAsync(m => m.OrdonnanceId == ordonnanceId);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            var medicament = await GetByIdAsync(id);
            if (medicament == null) return false;
            _context.Medicaments.Remove(medicament);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Medicament medicament)
        {
            if (medicament == null) throw new ArgumentNullException(nameof(medicament));
            return await DeleteAsync(medicament.Id);
        }

        private void ValidateMedicament(Medicament medicament)
        {
            if (string.IsNullOrWhiteSpace(medicament.Nom))
                throw new ArgumentException("Le nom du médicament est requis.", nameof(medicament.Nom));
            if (medicament.OrdonnanceId <= 0)
                throw new ArgumentException("L'ID de l'ordonnance doit être supérieur à 0.", nameof(medicament.OrdonnanceId));
            // La posologie est optionnelle
        }
    }
}
