using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    public interface IProthesisteService
    {
        Task<Prothesiste> CreateAsync(Prothesiste prothesiste);
        Task<Prothesiste?> GetByIdAsync(int id);
        Task<List<Prothesiste>> GetAllAsync();
        Task<List<Prothesiste>> GetByNameAsync(string name);
        Task<List<Prothesiste>> GetByPhoneAsync(string phone);
        Task<Prothesiste> UpdateAsync(Prothesiste prothesiste);
        Task<bool> ExistsAsync(string name);
        Task<int> CountAsync();
    }

    public class ProthesisteService : IProthesisteService
    {
        private readonly DentalContext _context;

        public ProthesisteService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Prothesiste> CreateAsync(Prothesiste prothesiste)
        {
            if (prothesiste == null) throw new ArgumentNullException(nameof(prothesiste));
            Validate(prothesiste);

            _context.Prothesistes.Add(prothesiste);
            await _context.SaveChangesAsync();
            return prothesiste;
        }

        public async Task<Prothesiste?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.Prothesistes
                .Include(p => p.CommandeProthesistes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Prothesiste>> GetAllAsync()
        {
            return await _context.Prothesistes
                .Include(p => p.CommandeProthesistes)
                .ToListAsync();
        }

        public async Task<List<Prothesiste>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Prothesiste>();
            var searchTerm = name.ToLower();
            return await _context.Prothesistes
                .Where(p => p.Nom.ToLower().Contains(searchTerm))
                .Include(p => p.CommandeProthesistes)
                .ToListAsync();
        }

        public async Task<List<Prothesiste>> GetByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return new List<Prothesiste>();
            var searchTerm = phone.ToLower();
            return await _context.Prothesistes
                .Where(p => p.Tel != null && p.Tel.ToLower().Contains(searchTerm))
                .Include(p => p.CommandeProthesistes)
                .ToListAsync();
        }

        public async Task<Prothesiste> UpdateAsync(Prothesiste prothesiste)
        {
            if (prothesiste == null) throw new ArgumentNullException(nameof(prothesiste));
            if (prothesiste.Id <= 0) throw new ArgumentException("L'ID est invalide.", nameof(prothesiste.Id));

            Validate(prothesiste);

            var existing = await GetByIdAsync(prothesiste.Id);
            if (existing == null) throw new InvalidOperationException($"Le prothésiste avec l'ID {prothesiste.Id} n'existe pas.");

            existing.Nom = prothesiste.Nom;
            existing.Adresse = prothesiste.Adresse;
            existing.Tel = prothesiste.Tel;

            _context.Prothesistes.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }


        public async Task<bool> ExistsAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return await _context.Prothesistes.AnyAsync(p => p.Nom == name);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Prothesistes.CountAsync();
        }

        private void Validate(Prothesiste p)
        {
            if (string.IsNullOrWhiteSpace(p.Nom)) throw new ArgumentException("Le nom est requis.", nameof(p.Nom));
            // Adresse/tel optional
        }
    }
}
