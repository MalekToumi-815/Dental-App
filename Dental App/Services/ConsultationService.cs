using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dental_App.Services
{
    /// <summary>
    /// Interface for Consultation service operations
    /// </summary>
    public interface IConsultationService
    {
        Task<Consultation> CreateAsync(Consultation consultation);
        Task<Consultation?> GetByIdAsync(int id);
        Task<List<Consultation>> GetAllAsync();
        Task<List<Consultation>> GetByPatientIdAsync(int patientId);
        Task<List<Consultation>> GetByDentIdAsync(int dentId);
        Task<List<Consultation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Consultation> UpdateAsync(Consultation consultation);
        Task<int> CountAsync();
        Task<int> CountByPatientAsync(int patientId);
        Task<bool> AddActesAsync(int consultationId, List<ActeMedical> actes);
        Task<List<ActeMedical>> GetActesByConsultationIdAsync(int consultationId);
    }

    /// <summary>
    /// Service for managing Consultation CRUD operations
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly DentalContext _context;

        public ConsultationService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create a new consultation
        /// </summary>
        public async Task<Consultation> CreateAsync(Consultation consultation)
        {
            if (consultation == null)
                throw new ArgumentNullException(nameof(consultation));

            ValidateConsultation(consultation);

            // Check if patient exists
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == consultation.PatientId);
            if (!patientExists)
                throw new InvalidOperationException($"Le patient avec l'ID {consultation.PatientId} n'existe pas.");

            // Check if dent exists (if specified)
            if (consultation.IdDent.HasValue)
            {
                var dentExists = await _context.Dents.AnyAsync(d => d.Id == consultation.IdDent.Value);
                if (!dentExists)
                    throw new InvalidOperationException($"La dent avec l'ID {consultation.IdDent} n'existe pas.");
            }

            // Set default date if not provided
            if (consultation.DateConsultation == null)
                consultation.DateConsultation = DateTime.Now;

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ Consultation créée : Id={consultation.Id}, PatientId={consultation.PatientId}");
            return consultation;
        }

        /// <summary>
        /// Get consultation by ID with related entities
        /// </summary>
        public async Task<Consultation?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));

            return await _context.Consultations
                .Include(c => c.Patient)
                .Include(c => c.IdDentNavigation)
                .Include(c => c.IdActes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Get all consultations with related entities
        /// </summary>
        public async Task<List<Consultation>> GetAllAsync()
        {
            return await _context.Consultations
                .Include(c => c.Patient)
                .Include(c => c.IdDentNavigation)
                .Include(c => c.IdActes)
                .OrderByDescending(c => c.DateConsultation)
                .ToListAsync();
        }

        /// <summary>
        /// Get all consultations for a specific patient
        /// </summary>
        public async Task<List<Consultation>> GetByPatientIdAsync(int patientId)
        {
            if (patientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            return await _context.Consultations
                .Where(c => c.PatientId == patientId)
                .Include(c => c.IdDentNavigation)
                .Include(c => c.IdActes)
                .OrderByDescending(c => c.DateConsultation)
                .ToListAsync();
        }

        /// <summary>
        /// Get all consultations for a specific dent
        /// </summary>
        public async Task<List<Consultation>> GetByDentIdAsync(int dentId)
        {
            if (dentId <= 0)
                throw new ArgumentException("Le DentId doit être supérieur à 0.", nameof(dentId));

            return await _context.Consultations
                .Where(c => c.IdDent == dentId)
                .Include(c => c.Patient)
                .Include(c => c.IdActes)
                .OrderByDescending(c => c.DateConsultation)
                .ToListAsync();
        }

        /// <summary>
        /// Get consultations within a date range
        /// </summary>
        public async Task<List<Consultation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("La date de début ne peut pas être supérieure à la date de fin.");

            return await _context.Consultations
                .Where(c => c.DateConsultation >= startDate && c.DateConsultation <= endDate)
                .Include(c => c.Patient)
                .Include(c => c.IdDentNavigation)
                .Include(c => c.IdActes)
                .OrderByDescending(c => c.DateConsultation)
                .ToListAsync();
        }

        /// <summary>
        /// Update an existing consultation
        /// </summary>
        public async Task<Consultation> UpdateAsync(Consultation consultation)
        {
            if (consultation == null)
                throw new ArgumentNullException(nameof(consultation));

            if (consultation.Id <= 0)
                throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(consultation.Id));

            ValidateConsultation(consultation);

            var existing = await GetByIdAsync(consultation.Id);
            if (existing == null)
                throw new InvalidOperationException($"La consultation avec l'ID {consultation.Id} n'existe pas.");

            // Check if dent exists (if specified)
            if (consultation.IdDent.HasValue && consultation.IdDent != existing.IdDent)
            {
                var dentExists = await _context.Dents.AnyAsync(d => d.Id == consultation.IdDent.Value);
                if (!dentExists)
                    throw new InvalidOperationException($"La dent avec l'ID {consultation.IdDent} n'existe pas.");
            }

            existing.PatientId = consultation.PatientId;
            existing.DateConsultation = consultation.DateConsultation;
            existing.Note = consultation.Note;
            existing.IdDent = consultation.IdDent;
            existing.MontantTotal = consultation.MontantTotal;

            _context.Consultations.Update(existing);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ Consultation mise à jour : Id={existing.Id}");
            return existing;
        }

        /// <summary>
        /// Get total count of consultations
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _context.Consultations.CountAsync();
        }

        /// <summary>
        /// Get consultation count for a specific patient
        /// </summary>
        public async Task<int> CountByPatientAsync(int patientId)
        {
            if (patientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            return await _context.Consultations.Where(c => c.PatientId == patientId).CountAsync();
        }

        /// <summary>
        /// Add multiple ActeMedical items to a consultation's navigation attribute
        /// </summary>
        public async Task<bool> AddActesAsync(int consultationId, List<ActeMedical> actes)
        {
            try
            {
                if (consultationId <= 0)
                    throw new ArgumentException("Le ConsultationId doit être supérieur à 0.", nameof(consultationId));

                if (actes == null || actes.Count == 0)
                    throw new ArgumentException("La liste des actes ne peut pas être null ou vide.", nameof(actes));

                var consultation = await _context.Consultations
                    .Include(c => c.IdActes)
                    .FirstOrDefaultAsync(c => c.Id == consultationId);

                if (consultation == null)
                    throw new InvalidOperationException($"La consultation avec l'ID {consultationId} n'existe pas.");

                // Validate that all actes exist
                var acteIds = actes.Select(a => a.Id).ToList();
                var validActes = await _context.ActeMedicals
                    .Where(a => acteIds.Contains(a.Id))
                    .ToListAsync();

                if (validActes.Count != actes.Count)
                    throw new InvalidOperationException("Un ou plusieurs actes médicaux n'existent pas dans la base de données.");

                // Add actes to the consultation (avoid duplicates)
                foreach (var acte in validActes)
                {
                    if (!consultation.IdActes.Any(a => a.Id == acte.Id))
                    {
                        consultation.IdActes.Add(acte);
                    }
                }

                _context.Consultations.Update(consultation);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✓ {validActes.Count} actes ajoutés à la consultation {consultationId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajout des actes à la consultation : {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get all ActeMedical items for a specific consultation
        /// </summary>
        public async Task<List<ActeMedical>> GetActesByConsultationIdAsync(int consultationId)
        {
            if (consultationId <= 0)
                throw new ArgumentException("Le ConsultationId doit être supérieur à 0.", nameof(consultationId));

            var consultation = await _context.Consultations
                .Include(c => c.IdActes)
                .FirstOrDefaultAsync(c => c.Id == consultationId);

            return consultation?.IdActes.OrderBy(a => a.Libelle).ToList() ?? new List<ActeMedical>();
        }

        /// <summary>
        /// Validates consultation properties
        /// </summary>
        private void ValidateConsultation(Consultation consultation)
        {
            if (consultation.PatientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(consultation.PatientId));

            if (consultation.MontantTotal.HasValue && consultation.MontantTotal < 0)
                throw new ArgumentException("Le montant total ne peut pas être négatif.", nameof(consultation.MontantTotal));
        }
    }
}
