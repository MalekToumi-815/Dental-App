using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    internal class CommandeProthesisteService
    {
        private readonly DentalContext _context;

        public CommandeProthesisteService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<CommandeProthesiste> CreateAsync(CommandeProthesiste commande)
        {
            if (commande == null) throw new ArgumentNullException(nameof(commande));
            // ensure FK set
            if (commande.IdProthesiste <= 0) throw new ArgumentException("L'IdProthesiste doit être défini.", nameof(commande.IdProthesiste));

            // Verify that the prothesiste exists in the database
            var prothExists = await _context.Prothesistes.AnyAsync(p => p.Id == commande.IdProthesiste);
            if (!prothExists) throw new InvalidOperationException($"Le prothésiste avec l'ID {commande.IdProthesiste} n'existe pas dans la base de données.");

            // set default date if not provided
            if (commande.Date == null) commande.Date = DateTime.Now;
            if (commande.SommePayees == null) commande.SommePayees = 0.0;

            _context.CommandeProthesistes.Add(commande);
            await _context.SaveChangesAsync();
            return commande;
        }

        public async Task<CommandeProthesiste?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être > 0.", nameof(id));
            return await _context.CommandeProthesistes
                .Include(c => c.IdProthesisteNavigation)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<CommandeProthesiste>> GetAllAsync()
        {
            return await _context.CommandeProthesistes
                .Include(c => c.IdProthesisteNavigation)
                .OrderByDescending(c => c.Date)
                .ToListAsync();
        }

        public async Task<List<CommandeProthesiste>> GetByProthesisteAsync(int prothesisteId)
        {
            if (prothesisteId <= 0) throw new ArgumentException("Le prothesisteId doit être > 0.", nameof(prothesisteId));
            return await _context.CommandeProthesistes
                .Where(c => c.IdProthesiste == prothesisteId)
                .Include(c => c.IdProthesisteNavigation)
                .OrderByDescending(c => c.Date)
                .ToListAsync();
        }

        public async Task<CommandeProthesiste> UpdateAsync(CommandeProthesiste commande)
        {
            if (commande == null) throw new ArgumentNullException(nameof(commande));
            if (commande.Id <= 0) throw new ArgumentException("L'ID est invalide.", nameof(commande.Id));

            var existing = await GetByIdAsync(commande.Id);
            if (existing == null) throw new InvalidOperationException($"La commande avec l'ID {commande.Id} n'a pas été trouvée.");

            existing.Date = commande.Date ?? existing.Date;
            existing.Achats = commande.Achats;
            existing.SommePayees = commande.SommePayees ?? existing.SommePayees;
            
            // allow changing prothesiste FK, but verify it exists
            if (commande.IdProthesiste > 0 && commande.IdProthesiste != existing.IdProthesiste)
            {
                var prothExists = await _context.Prothesistes.AnyAsync(p => p.Id == commande.IdProthesiste);
                if (!prothExists) throw new InvalidOperationException($"Le prothésiste avec l'ID {commande.IdProthesiste} n'existe pas dans la base de données.");
                existing.IdProthesiste = commande.IdProthesiste;
            }

            _context.CommandeProthesistes.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être > 0.", nameof(id));
            var existing = await _context.CommandeProthesistes.FindAsync(id);
            if (existing == null) return false;
            _context.CommandeProthesistes.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountAsync()
        {
            return await _context.CommandeProthesistes.CountAsync();
        }

        /// <summary>
        /// Lie une commande existante à un prothésiste (met à jour la FK et les navigations).
        /// Retourne true si le lien a été ajouté ou mis à jour, false si la commande était déjà liée au même prothésiste.
        /// </summary>
        public async Task<bool> AjouterCommandeAProthesisteAsync(int prothesisteId, int commandeId)
        {
            if (prothesisteId <= 0) throw new ArgumentException("Le prothesisteId doit être > 0.", nameof(prothesisteId));
            if (commandeId <= 0) throw new ArgumentException("Le commandeId doit être > 0.", nameof(commandeId));

            // Load prothesiste with commandes
            var proth = await _context.Prothesistes
                .Include(p => p.CommandeProthesistes)
                .FirstOrDefaultAsync(p => p.Id == prothesisteId);
            if (proth == null) throw new InvalidOperationException($"Le prothésiste {prothesisteId} n'a pas été trouvé.");

            // Load commande with its navigation
            var commande = await _context.CommandeProthesistes
                .Include(c => c.IdProthesisteNavigation)
                .FirstOrDefaultAsync(c => c.Id == commandeId);
            if (commande == null) throw new InvalidOperationException($"La commande {commandeId} n'a pas été trouvée.");

            // If already linked to the same prothesiste, nothing to do
            if (commande.IdProthesiste == prothesisteId)
                return false;

            // If commande was linked to another prothesiste, remove from its collection to keep navigations consistent
            if (commande.IdProthesiste != 0 && commande.IdProthesisteNavigation != null)
            {
                var old = await _context.Prothesistes
                    .Include(p => p.CommandeProthesistes)
                    .FirstOrDefaultAsync(p => p.Id == commande.IdProthesiste);
                if (old != null && old.CommandeProthesistes != null)
                {
                    var toRemove = old.CommandeProthesistes.FirstOrDefault(c => c.Id == commande.Id);
                    if (toRemove != null) old.CommandeProthesistes.Remove(toRemove);
                }
            }

            // Set FK and navigation
            commande.IdProthesiste = prothesisteId;
            commande.IdProthesisteNavigation = proth;

            if (proth.CommandeProthesistes == null)
                proth.CommandeProthesistes = new List<CommandeProthesiste>();

            if (!proth.CommandeProthesistes.Any(c => c.Id == commande.Id))
                proth.CommandeProthesistes.Add(commande);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
