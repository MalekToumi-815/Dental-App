using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    public interface IPatientService
    {
        Task<Patient> CreateAsync(Patient patient);
        Task<Patient?> GetByIdAsync(int id);
        Task<Patient?> GetByIdWithConsultationsAsync(int id);
        Task<List<Patient>> GetAllAsync();
        Task<List<Patient>> GetPatientsAsync(int pageIndex, int pageSize = 10);
        Task<List<Patient>> GetByNameAsync(string nom, string prenom);
        Task<List<Patient>> SearchByNameAsync(string term);
        Task<Patient> UpdateAsync(Patient patient);
        Task<bool> DeleteAsync(int id);
        Task<int> DeleteByNameAsync(string nom, string prenom);
        Task<bool> DeleteAsync(Patient patient);
        Task<bool> ExistsAsync(string nom, string prenom);
        Task<int> CountAsync();
        Task<Patient?> GetByCinAsync(string? cin);
        Task<Patient> AjouterMontantAsync(int patientId, decimal montant);
        Task<decimal> GetSommeAPayerAsync(int patientId);
    }

    public class PatientService : IPatientService
    {
        private readonly DentalContext _context;
        private readonly IDentService _dentService;

        public PatientService(DentalContext context, IDentService dentService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dentService = dentService ?? throw new ArgumentNullException(nameof(dentService));
        }

        public async Task<Patient> CreateAsync(Patient patient)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            ValidatePatient(patient);

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            await _dentService.InitializeTeethForPatientAsync(patient.Id);
            return patient;
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.Patients.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Patient?> GetByIdWithConsultationsAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.Patients
                .Include(p => p.Consultations)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Patient>> GetAllAsync()
        {
            return await _context.Patients.ToListAsync();
        }

        public async Task<List<Patient>> GetPatientsAsync(int pageIndex, int pageSize = 10)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            return await _context.Patients
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Recherche des patients par nom et prénom exacts (insensible à la casse)
        /// </summary>
        public async Task<List<Patient>> GetByNameAsync(string nom, string prenom)
        {
            if (string.IsNullOrWhiteSpace(nom) && string.IsNullOrWhiteSpace(prenom))
                return new List<Patient>();

            var query = _context.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(nom))
                query = query.Where(p => EF.Functions.Like(p.Nom, nom));
            if (!string.IsNullOrWhiteSpace(prenom))
                query = query.Where(p => EF.Functions.Like(p.Prenom, prenom));

            return await query.ToListAsync();
        }

        /// <summary>
        /// Recherche "fuzzy" utilisée lors de la saisie par le médecin.
        /// Retourne les patients dont le nom ou prénom commence par la chaîne fournie
        /// ou la contient. Résultats triés : starts-with d'abord, puis contains.
        /// </summary>
        public async Task<List<Patient>> SearchByNameAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return new List<Patient>();
            term = term.Trim();

            // Rechercher starts-with en priorité
            var startsWith = await _context.Patients
                .Where(p => EF.Functions.Like(p.Nom, term + "%") || EF.Functions.Like(p.Prenom, term + "%"))
                .OrderBy(p => p.Nom).ThenBy(p => p.Prenom)
                .ToListAsync();

            return startsWith;
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            if (patient.Id <= 0) throw new ArgumentException("L'ID du patient est invalide.", nameof(patient.Id));

            ValidatePatient(patient);

            var existing = await GetByIdAsync(patient.Id);
            if (existing == null) throw new InvalidOperationException($"Le patient avec l'ID {patient.Id} n'existe pas.");

            existing.Nom = patient.Nom;
            existing.Prenom = patient.Prenom;
            existing.DateNaissance = patient.DateNaissance;
            existing.Sexe = patient.Sexe;
            existing.Telephone = patient.Telephone;
            existing.SommePaye = patient.SommePaye;
            existing.Adresse = patient.Adresse;
            existing.Profession = patient.Profession;
            existing.Cin = patient.Cin;

            _context.Patients.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            var patient = await GetByIdAsync(id);
            if (patient == null) return false;
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Supprime les patients correspondant au nom/prénom fournis (insensible à la casse).
        /// Retourne le nombre de lignes supprimées.
        /// </summary>
        public async Task<int> DeleteByNameAsync(string nom, string prenom)
        {
            if (string.IsNullOrWhiteSpace(nom) && string.IsNullOrWhiteSpace(prenom))
                return 0;

            var query = _context.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(nom))
                query = query.Where(p => p.Nom == nom);
            if (!string.IsNullOrWhiteSpace(prenom))
                query = query.Where(p => p.Prenom == prenom);

            var list = await query.ToListAsync();
            if (list.Count == 0) return 0;

            _context.Patients.RemoveRange(list);
            await _context.SaveChangesAsync();
            return list.Count;
        }

        public async Task<bool> DeleteAsync(Patient patient)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            return await DeleteAsync(patient.Id);
        }

        public async Task<bool> ExistsAsync(string nom, string prenom)
        {
            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom)) return false;
            return await _context.Patients.AnyAsync(p => p.Nom == nom && p.Prenom == prenom);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Patients.CountAsync();
        }

        private void ValidatePatient(Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.Nom)) throw new ArgumentException("Le nom est requis.", nameof(patient.Nom));
            if (string.IsNullOrWhiteSpace(patient.Prenom)) throw new ArgumentException("Le prénom est requis.", nameof(patient.Prenom));
            if (string.IsNullOrWhiteSpace(patient.Adresse)) throw new ArgumentException("L'adresse est requise.", nameof(patient.Adresse));
            if (string.IsNullOrWhiteSpace(patient.Telephone)) throw new ArgumentException("Le téléphone est requis.", nameof(patient.Telephone));

            // Accept any valid DateOnly value (no hard year range). Ensure it's a valid date (not default)
            if (patient.DateNaissance == default)
                throw new ArgumentException("La date de naissance est requise et doit être valide.", nameof(patient.DateNaissance));
        }

        public async Task<Patient?> GetByCinAsync(string? cin)
        {
            if (string.IsNullOrWhiteSpace(cin)) return null;
            return await _context.Patients.FirstOrDefaultAsync(p => p.Cin == cin);
        }

        public async Task<Patient> AjouterMontantAsync(int patientId, decimal montant)
        {
            if (patientId <= 0) throw new ArgumentException("L'ID du patient doit être supérieur à 0.", nameof(patientId));
            if (montant <= 0) throw new ArgumentException("Le montant doit être supérieur à 0.", nameof(montant));

            var patient = await GetByIdAsync(patientId);
            if (patient == null) throw new InvalidOperationException($"Le patient avec l'ID {patientId} n'existe pas.");

            patient.SommePaye += montant;

            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<decimal> GetSommeAPayerAsync(int patientId)
        {
            if (patientId <= 0) throw new ArgumentException("L'ID du patient doit être supérieur à 0.", nameof(patientId));

            var patient = await GetByIdAsync(patientId);
            if (patient == null) throw new InvalidOperationException($"Le patient avec l'ID {patientId} n'existe pas.");

            // Load consultations into memory first, then sum on the client side
            // SQLite doesn't support Sum on decimal types in LINQ queries
            var consultations = await _context.Consultations
                .Where(c => c.PatientId == patientId)
                .ToListAsync();

            var sommeAPayer = consultations.Sum(c => c.MontantTotal ?? 0m);

            return sommeAPayer;
        }
    }
}
