using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    internal class UserService
    {
        private readonly DentalContext _context;

        public UserService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Crée un nouvel utilisateur dans la base de données
        /// </summary>
        /// <param name="utilisateur">L'utilisateur à créer</param>
        /// <returns>L'utilisateur créé avec son ID généré</returns>
        public async Task<Utilisateur> CreateAsync(Utilisateur utilisateur)
        {
            if (utilisateur == null)
                throw new ArgumentNullException(nameof(utilisateur));

            ValidateUtilisateur(utilisateur);

            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();
            return utilisateur;
        }

        /// <summary>
        /// Récupère un utilisateur par son ID
        /// </summary>
        /// <param name="id">L'ID de l'utilisateur</param>
        /// <returns>L'utilisateur trouvé ou null</returns>
        public async Task<Utilisateur> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));

            return await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Récupère tous les utilisateurs
        /// </summary>
        /// <returns>Liste de tous les utilisateurs</returns>
        public async Task<List<Utilisateur>> GetAllAsync()
        {
            return await _context.Utilisateurs.ToListAsync();
        }

        /// <summary>
        /// Récupère un utilisateur par son nom et prénom
        /// </summary>
        /// <param name="nom">Le nom de l'utilisateur</param>
        /// <param name="prenom">Le prénom de l'utilisateur</param>
        /// <returns>L'utilisateur trouvé ou null</returns>
        public async Task<Utilisateur> GetByNomPrenomAsync(string nom, string prenom)
        {
            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom))
                throw new ArgumentException("Le nom et le prénom ne peuvent pas être vides.");

            return await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Nom == nom && u.Prenom == prenom);
        }

        /// <summary>
        /// Met à jour un utilisateur existant
        /// </summary>
        /// <param name="utilisateur">L'utilisateur à mettre à jour (doit avoir un ID valide)</param>
        /// <returns>L'utilisateur mis à jour</returns>
        public async Task<Utilisateur> UpdateAsync(Utilisateur utilisateur)
        {
            if (utilisateur == null)
                throw new ArgumentNullException(nameof(utilisateur));

            if (utilisateur.Id <= 0)
                throw new ArgumentException("L'ID de l'utilisateur est invalide.", nameof(utilisateur.Id));

            ValidateUtilisateur(utilisateur);

            var existingUser = await GetByIdAsync(utilisateur.Id);
            if (existingUser == null)
                throw new InvalidOperationException($"L'utilisateur avec l'ID {utilisateur.Id} n'existe pas.");

            existingUser.Nom = utilisateur.Nom;
            existingUser.Prenom = utilisateur.Prenom;
            existingUser.MotDePasseHash = utilisateur.MotDePasseHash;

            _context.Utilisateurs.Update(existingUser);
            await _context.SaveChangesAsync();
            return existingUser;
        }

        /// <summary>
        /// Supprime un utilisateur par son ID
        /// </summary>
        /// <param name="id">L'ID de l'utilisateur à supprimer</param>
        /// <returns>True si suppression réussie, False sinon</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));

            var utilisateur = await GetByIdAsync(id);
            if (utilisateur == null)
                return false;

            _context.Utilisateurs.Remove(utilisateur);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Supprime un utilisateur
        /// </summary>
        /// <param name="utilisateur">L'utilisateur à supprimer</param>
        /// <returns>True si suppression réussie, False sinon</returns>
        public async Task<bool> DeleteAsync(Utilisateur utilisateur)
        {
            if (utilisateur == null)
                throw new ArgumentNullException(nameof(utilisateur));

            return await DeleteAsync(utilisateur.Id);
        }

        /// <summary>
        /// Vérifie si un utilisateur avec le nom et prénom donné existe
        /// </summary>
        /// <param name="nom">Le nom</param>
        /// <param name="prenom">Le prénom</param>
        /// <returns>True si existe, False sinon</returns>
        public async Task<bool> ExistsAsync(string nom, string prenom)
        {
            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom))
                return false;

            return await _context.Utilisateurs
                .AnyAsync(u => u.Nom == nom && u.Prenom == prenom);
        }

        /// <summary>
        /// Récupère le nombre total d'utilisateurs
        /// </summary>
        /// <returns>Le nombre d'utilisateurs</returns>
        public async Task<int> CountAsync()
        {
            return await _context.Utilisateurs.CountAsync();
        }

        /// <summary>
        /// Valide les propriétés d'un utilisateur
        /// </summary>
        private void ValidateUtilisateur(Utilisateur utilisateur)
        {
            if (string.IsNullOrWhiteSpace(utilisateur.Nom))
                throw new ArgumentException("Le nom est requis.", nameof(utilisateur.Nom));

            if (string.IsNullOrWhiteSpace(utilisateur.Prenom))
                throw new ArgumentException("Le prénom est requis.", nameof(utilisateur.Prenom));

            if (string.IsNullOrWhiteSpace(utilisateur.MotDePasseHash))
                throw new ArgumentException("Le mot de passe est requis.", nameof(utilisateur.MotDePasseHash));
        }
    }
}
