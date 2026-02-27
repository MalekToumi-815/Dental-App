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
            if (commande.IdProthesiste <= 0) throw new ArgumentException("IdProthesiste must be set.", nameof(commande.IdProthesiste));

            // Verify that the prothesiste exists in the database
            var prothExists = await _context.Prothesistes.AnyAsync(p => p.Id == commande.IdProthesiste);
            if (!prothExists) throw new InvalidOperationException($"Prothesiste with Id {commande.IdProthesiste} does not exist in the database.");

            // set default date if not provided
            if (commande.Date == null) commande.Date = DateTime.Now;
            if (commande.SommePayees == null) commande.SommePayees = 0.0;

            _context.CommandeProthesistes.Add(commande);
            await _context.SaveChangesAsync();
            return commande;
        }

        public async Task<CommandeProthesiste?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("Id must be > 0", nameof(id));
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
            if (prothesisteId <= 0) throw new ArgumentException("prothesisteId must be > 0", nameof(prothesisteId));
            return await _context.CommandeProthesistes
                .Where(c => c.IdProthesiste == prothesisteId)
                .Include(c => c.IdProthesisteNavigation)
                .OrderByDescending(c => c.Date)
                .ToListAsync();
        }

        public async Task<CommandeProthesiste> UpdateAsync(CommandeProthesiste commande)
        {
            if (commande == null) throw new ArgumentNullException(nameof(commande));
            if (commande.Id <= 0) throw new ArgumentException("Id is invalid", nameof(commande.Id));

            var existing = await GetByIdAsync(commande.Id);
            if (existing == null) throw new InvalidOperationException($"Commande with Id {commande.Id} not found");

            existing.Date = commande.Date ?? existing.Date;
            existing.Achats = commande.Achats;
            existing.SommePayees = commande.SommePayees ?? existing.SommePayees;
            
            // allow changing prothesiste FK, but verify it exists
            if (commande.IdProthesiste > 0 && commande.IdProthesiste != existing.IdProthesiste)
            {
                var prothExists = await _context.Prothesistes.AnyAsync(p => p.Id == commande.IdProthesiste);
                if (!prothExists) throw new InvalidOperationException($"Prothesiste with Id {commande.IdProthesiste} does not exist in the database.");
                existing.IdProthesiste = commande.IdProthesiste;
            }

            _context.CommandeProthesistes.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("Id must be > 0", nameof(id));
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
            if (prothesisteId <= 0) throw new ArgumentException("prothesisteId must be > 0", nameof(prothesisteId));
            if (commandeId <= 0) throw new ArgumentException("commandeId must be > 0", nameof(commandeId));

            // Load prothesiste with commandes
            var proth = await _context.Prothesistes
                .Include(p => p.CommandeProthesistes)
                .FirstOrDefaultAsync(p => p.Id == prothesisteId);
            if (proth == null) throw new InvalidOperationException($"Prothesiste {prothesisteId} not found");

            // Load commande with its navigation
            var commande = await _context.CommandeProthesistes
                .Include(c => c.IdProthesisteNavigation)
                .FirstOrDefaultAsync(c => c.Id == commandeId);
            if (commande == null) throw new InvalidOperationException($"Commande {commandeId} not found");

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
