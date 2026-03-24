using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    public interface IAntecedentService
    {
        Task<Antecedant> CreateAsync(Antecedant antecedant);
        Task<Antecedant?> GetByIdAsync(int id);
        Task<List<Antecedant>> GetAllAsync();
        Task<List<Antecedant>> GetByNameAsync(string name);
        Task<Antecedant> UpdateAsync(Antecedant antecedant);
        Task<bool> DeleteAsync(int id);
        Task<int> DeleteByNameAsync(string name);
        Task<bool> ExistsAsync(string name);
        Task<int> CountAsync();
        Task<bool> AjouterAntecedantAsync(int patientId, int antecedantId);
    }

    internal class AntecedentService : IAntecedentService
    {
        private readonly DentalContext _context;

        public AntecedentService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Antecedant> CreateAsync(Antecedant antecedant)
        {
            if (antecedant == null) throw new ArgumentNullException(nameof(antecedant));
            Validate(antecedant);

            _context.Antecedants.Add(antecedant);
            await _context.SaveChangesAsync();
            return antecedant;
        }

        public async Task<Antecedant?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.Antecedants.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Antecedant>> GetAllAsync()
        {
            return await _context.Antecedants.ToListAsync();
        }

        public async Task<List<Antecedant>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Antecedant>();
            return await _context.Antecedants.Where(a => a.Nom == name).ToListAsync();
        }

        public async Task<Antecedant> UpdateAsync(Antecedant antecedant)
        {
            if (antecedant == null) throw new ArgumentNullException(nameof(antecedant));
            if (antecedant.Id <= 0) throw new ArgumentException("L'ID est invalide.", nameof(antecedant.Id));

            Validate(antecedant);

            var existing = await GetByIdAsync(antecedant.Id);
            if (existing == null) throw new InvalidOperationException($"L'antecedant avec l'ID {antecedant.Id} n'existe pas.");

            existing.Nom = antecedant.Nom;
            existing.Description = antecedant.Description;

            _context.Antecedants.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            var existing = await GetByIdAsync(id);
            if (existing == null) return false;
            _context.Antecedants.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            var list = await _context.Antecedants.Where(a => a.Nom == name).ToListAsync();
            if (list.Count == 0) return 0;
            _context.Antecedants.RemoveRange(list);
            await _context.SaveChangesAsync();
            return list.Count;
        }

        public async Task<bool> ExistsAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return await _context.Antecedants.AnyAsync(a => a.Nom == name);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Antecedants.CountAsync();
        }

        /// <summary>
        /// Lie un antécédent à un patient (many-to-many).
        /// Retourne true si le lien a été ajouté, false si le lien existait déjà.
        /// Lance une exception si le patient ou l'antécédent n'existe pas.
        /// </summary>
        public async Task<bool> AjouterAntecedantAsync(int patientId, int antecedantId)
        {
            if (patientId <= 0) throw new ArgumentException("PatientId invalide.", nameof(patientId));
            if (antecedantId <= 0) throw new ArgumentException("AntecedantId invalide.", nameof(antecedantId));

            // Charger le patient avec ses antecedants
            var patient = await _context.Patients
                .Include(p => p.IdAntecedants)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) throw new InvalidOperationException($"Patient avec l'ID {patientId} introuvable.");

            var antecedant = await _context.Antecedants
                .Include(a => a.IdPatients)
                .FirstOrDefaultAsync(a => a.Id == antecedantId);

            if (antecedant == null) throw new InvalidOperationException($"Antecedant avec l'ID {antecedantId} introuvable.");

            // Vérifier si le lien existe déjà (côté patient ou côté antecedant)
            if (patient.IdAntecedants.Any(a => a.Id == antecedantId) || antecedant.IdPatients.Any(p => p.Id == patientId))
                return false; // déjà lié

            // Ajouter le lien sur le côté patient
            patient.IdAntecedants.Add(antecedant);

            await _context.SaveChangesAsync();
            return true;
        }

        private void Validate(Antecedant antecedant)
        {
            if (string.IsNullOrWhiteSpace(antecedant.Nom)) throw new ArgumentException("Le nom est requis.", nameof(antecedant.Nom));
            // Description optional
        }
    }
}
