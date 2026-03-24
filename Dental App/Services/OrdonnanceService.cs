using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dental_App.Services
{
    /// <summary>
    /// Interface for Ordonnance service operations
    /// </summary>
    public interface IOrdonnanceService
    {
        Task<Ordonnance> CreateAsync(Ordonnance ordonnance);
        Task<Ordonnance?> GetByIdAsync(int id);
        Task<List<Ordonnance>> GetAllAsync();
        Task<List<Ordonnance>> GetByPatientIdAsync(int patientId);
        Task<List<Ordonnance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Ordonnance> UpdateAsync(Ordonnance ordonnance);
        Task<int> CountAsync();
        Task<int> CountByPatientAsync(int patientId);
        Task<bool> AddMedicamentAsync(int ordonnanceId, Medicament medicament);
    }

    /// <summary>
    /// Service for managing Ordonnance CRUD operations
    /// </summary>
    public class OrdonnanceService : IOrdonnanceService
    {
        private readonly DentalContext _context;

        public OrdonnanceService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create a new ordonnance
        /// </summary>
        public async Task<Ordonnance> CreateAsync(Ordonnance ordonnance)
        {
            if (ordonnance == null)
                throw new ArgumentNullException(nameof(ordonnance));

            ValidateOrdonnance(ordonnance);

            // Check if patient exists
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == ordonnance.PatientId);
            if (!patientExists)
                throw new InvalidOperationException($"Le patient avec l'ID {ordonnance.PatientId} n'existe pas.");

            // Set default date if not provided
            if (ordonnance.DateCreation == null)
                ordonnance.DateCreation = DateTime.Now;

            _context.Ordonnances.Add(ordonnance);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ Ordonnance créée : Id={ordonnance.Id}, PatientId={ordonnance.PatientId}");
            return ordonnance;
        }

        /// <summary>
        /// Get ordonnance by ID with related entities
        /// </summary>
        public async Task<Ordonnance?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));

            return await _context.Ordonnances
                .Include(o => o.Patient)
                .Include(o => o.Medicaments)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>
        /// Get all ordonnances with related entities
        /// </summary>
        public async Task<List<Ordonnance>> GetAllAsync()
        {
            return await _context.Ordonnances
                .Include(o => o.Patient)
                .Include(o => o.Medicaments)
                .OrderByDescending(o => o.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Get all ordonnances for a specific patient
        /// </summary>
        public async Task<List<Ordonnance>> GetByPatientIdAsync(int patientId)
        {
            if (patientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            return await _context.Ordonnances
                .Where(o => o.PatientId == patientId)
                .Include(o => o.Medicaments)
                .OrderByDescending(o => o.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Get ordonnances within a date range
        /// </summary>
        public async Task<List<Ordonnance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("La date de début ne peut pas être supérieure à la date de fin.");

            return await _context.Ordonnances
                .Where(o => o.DateCreation >= startDate && o.DateCreation <= endDate)
                .Include(o => o.Patient)
                .Include(o => o.Medicaments)
                .OrderByDescending(o => o.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Update an existing ordonnance
        /// </summary>
        public async Task<Ordonnance> UpdateAsync(Ordonnance ordonnance)
        {
            if (ordonnance == null)
                throw new ArgumentNullException(nameof(ordonnance));

            if (ordonnance.Id <= 0)
                throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(ordonnance.Id));

            ValidateOrdonnance(ordonnance);

            var existing = await GetByIdAsync(ordonnance.Id);
            if (existing == null)
                throw new InvalidOperationException($"L'ordonnance avec l'ID {ordonnance.Id} n'existe pas.");

            existing.PatientId = ordonnance.PatientId;
            existing.DateCreation = ordonnance.DateCreation;

            _context.Ordonnances.Update(existing);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ Ordonnance mise à jour : Id={existing.Id}");
            return existing;
        }

        /// <summary>
        /// Get total count of ordonnances
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _context.Ordonnances.CountAsync();
        }

        /// <summary>
        /// Get ordonnance count for a specific patient
        /// </summary>
        public async Task<int> CountByPatientAsync(int patientId)
        {
            if (patientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            return await _context.Ordonnances.Where(o => o.PatientId == patientId).CountAsync();
        }

        /// <summary>
        /// Add a single medicament to an ordonnance
        /// </summary>
        public async Task<bool> AddMedicamentAsync(int ordonnanceId, Medicament medicament)
        {
            try
            {
                if (ordonnanceId <= 0)
                    throw new ArgumentException("L'OrdonnanceId doit être supérieur à 0.", nameof(ordonnanceId));

                if (medicament == null)
                    throw new ArgumentNullException(nameof(medicament));

                var ordonnance = await _context.Ordonnances
                    .Include(o => o.Medicaments)
                    .FirstOrDefaultAsync(o => o.Id == ordonnanceId);

                if (ordonnance == null)
                    throw new InvalidOperationException($"L'ordonnance avec l'ID {ordonnanceId} n'existe pas.");

                // Validate medicament
                if (string.IsNullOrWhiteSpace(medicament.Nom))
                    throw new ArgumentException("Le nom du médicament est requis.", nameof(medicament.Nom));

                // Add medicament to ordonnance's collection
                _context.Medicaments.Add(medicament); 
                ordonnance.Medicaments.Add(medicament);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✓ Médicament '{medicament.Nom}' ajouté à l'ordonnance {ordonnanceId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajout du médicament à l'ordonnance : {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates ordonnance properties
        /// </summary>
        private void ValidateOrdonnance(Ordonnance ordonnance)
        {
            if (ordonnance.PatientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(ordonnance.PatientId));
        }
    }
}
