using Dental_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services
{
    /// <summary>
    /// Status constants for RendezVous
    /// </summary>
    public static class RendezVousStatus
    {
        public const string EnAttente = "en attente";
        public const string Termine = "termine";
        public const string Annule = "annule";
    }

    public interface IRendezVousService
    {
        Task<RendezVou> CreateAsync(RendezVou rendezVous);
        Task<RendezVou?> GetByIdAsync(int id);
        Task<List<RendezVou>> GetAllAsync();
        Task<List<RendezVou>> GetByPatientIdAsync(int patientId);
        Task<List<RendezVou>> GetByStatusAsync(string status);
        Task<List<RendezVou>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<RendezVou>> GetUpcomingAsync(int patientId);
        Task<RendezVou> UpdateAsync(RendezVou rendezVous);
        Task<bool> UpdateStatusAsync(int id, string newStatus);
        Task<bool> CompleteAsync(int id);
        Task<bool> CancelAsync(int id);
        Task<int> CountAsync();
        Task<int> CountByPatientAsync(int patientId);
        Task<bool> HasConflictAsync(int patientId, DateTime dateDebut, int? excludeId = null);
    }

    public class RendezVousService : IRendezVousService
    {
        private readonly DentalContext _context;

        public RendezVousService(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create a new appointment (RendezVous)
        /// </summary>
        public async Task<RendezVou> CreateAsync(RendezVou rendezVous)
        {
            if (rendezVous == null) throw new ArgumentNullException(nameof(rendezVous));
            if (rendezVous.PatientId <= 0) throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(rendezVous.PatientId));

            ValidateRendezVous(rendezVous);

            // Check if patient exists
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == rendezVous.PatientId);
            if (!patientExists)
                throw new InvalidOperationException($"Le patient avec l'ID {rendezVous.PatientId} n'existe pas.");

            // Check for scheduling conflicts
            var conflict = await HasConflictAsync(rendezVous.PatientId, rendezVous.DateDebut);
            if (conflict)
                throw new InvalidOperationException($"Un rendez-vous est déjà programmé à cette date/heure.");

            // Set default status if not provided
            if (string.IsNullOrWhiteSpace(rendezVous.Statut))
                rendezVous.Statut = RendezVousStatus.EnAttente;

            _context.RendezVous.Add(rendezVous);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ RendezVous créé : Id={rendezVous.Id}, PatientId={rendezVous.PatientId}");
            return rendezVous;
        }

        /// <summary>
        /// Get appointment by ID
        /// </summary>
        public async Task<RendezVou?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            return await _context.RendezVous.Include(r => r.Patient).FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Get all appointments
        /// </summary>
        public async Task<List<RendezVou>> GetAllAsync()
        {
            return await _context.RendezVous.Include(r => r.Patient).OrderBy(r => r.DateDebut).ToListAsync();
        }

        /// <summary>
        /// Get all appointments for a specific patient
        /// </summary>
        public async Task<List<RendezVou>> GetByPatientIdAsync(int patientId)
        {
            if (patientId <= 0) throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));
            return await _context.RendezVous
                .Where(r => r.PatientId == patientId)
                .OrderBy(r => r.DateDebut)
                .ToListAsync();
        }

        /// <summary>
        /// Get appointments by status ("en attente", "termine", "annule")
        /// </summary>
        public async Task<List<RendezVou>> GetByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Le statut ne peut pas être vide.", nameof(status));
            ValidateStatus(status);
            return await _context.RendezVous
                .Where(r => r.Statut == status)
                .OrderBy(r => r.DateDebut)
                .ToListAsync();
        }

        /// <summary>
        /// Get appointments within a date range
        /// </summary>
        public async Task<List<RendezVou>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("La date de début ne peut pas être supérieure à la date de fin.");

            return await _context.RendezVous
                .Where(r => r.DateDebut >= startDate && r.DateDebut <= endDate)
                .OrderBy(r => r.DateDebut)
                .ToListAsync();
        }

        /// <summary>
        /// Get upcoming appointments for a patient (not completed or cancelled)
        /// </summary>
        public async Task<List<RendezVou>> GetUpcomingAsync(int patientId)
        {
            if (patientId <= 0) throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            var now = DateTime.Now;
            return await _context.RendezVous
                .Where(r => r.PatientId == patientId 
                    && r.DateDebut >= now 
                    && r.Statut != RendezVousStatus.Termine 
                    && r.Statut != RendezVousStatus.Annule)
                .OrderBy(r => r.DateDebut)
                .ToListAsync();
        }

        /// <summary>
        /// Update an entire appointment record
        /// </summary>
        public async Task<RendezVou> UpdateAsync(RendezVou rendezVous)
        {
            if (rendezVous == null) throw new ArgumentNullException(nameof(rendezVous));
            if (rendezVous.Id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(rendezVous.Id));
            if (rendezVous.PatientId <= 0) throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(rendezVous.PatientId));

            ValidateRendezVous(rendezVous);

            var existing = await GetByIdAsync(rendezVous.Id);
            if (existing == null)
                throw new InvalidOperationException($"Le rendez-vous avec l'ID {rendezVous.Id} n'existe pas.");

            // Check for scheduling conflicts (excluding current appointment)
            var conflict = await HasConflictAsync(rendezVous.PatientId, rendezVous.DateDebut, rendezVous.Id);
            if (conflict)
                throw new InvalidOperationException($"Un rendez-vous est déjà programmé à cette date/heure.");

            existing.PatientId = rendezVous.PatientId;
            existing.DateDebut = rendezVous.DateDebut;
            existing.Statut = rendezVous.Statut ?? RendezVousStatus.EnAttente;

            _context.RendezVous.Update(existing);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ RendezVous mis à jour : Id={existing.Id}");
            return existing;
        }

        /// <summary>
        /// Update only the status of an appointment
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int id, string newStatus)
        {
            if (id <= 0) throw new ArgumentException("L'ID doit être supérieur à 0.", nameof(id));
            if (string.IsNullOrWhiteSpace(newStatus)) throw new ArgumentException("Le statut ne peut pas être vide.", nameof(newStatus));
            ValidateStatus(newStatus);

            var rendezVous = await GetByIdAsync(id);
            if (rendezVous == null)
            {
                System.Diagnostics.Debug.WriteLine($"Le rendez-vous avec l'ID {id} n'a pas été trouvé.");
                return false;
            }

            rendezVous.Statut = newStatus;
            _context.RendezVous.Update(rendezVous);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"✓ Statut du rendez-vous {id} mis à jour à : {newStatus}");
            return true;
        }

        /// <summary>
        /// Mark appointment as completed
        /// </summary>
        public async Task<bool> CompleteAsync(int id)
        {
            return await UpdateStatusAsync(id, RendezVousStatus.Termine);
        }

        /// <summary>
        /// Cancel an appointment
        /// </summary>
        public async Task<bool> CancelAsync(int id)
        {
            return await UpdateStatusAsync(id, RendezVousStatus.Annule);
        }

        /// <summary>
        /// Get total count of appointments
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _context.RendezVous.CountAsync();
        }

        /// <summary>
        /// Get appointment count for a specific patient
        /// </summary>
        public async Task<int> CountByPatientAsync(int patientId)
        {
            if (patientId <= 0) throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));
            return await _context.RendezVous.Where(r => r.PatientId == patientId).CountAsync();
        }

        /// <summary>
        /// Check if there's already an appointment at the specified date/time (doctor can't have multiple patients at same time)
        /// </summary>
        public async Task<bool> HasConflictAsync(int patientId, DateTime dateDebut, int? excludeId = null)
        {
            if (patientId <= 0) throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            var query = _context.RendezVous.Where(r =>
                r.Statut != RendezVousStatus.Annule &&
                r.DateDebut == dateDebut);

            if (excludeId.HasValue)
                query = query.Where(r => r.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        private void ValidateRendezVous(RendezVou rendezVous)
        {
            if (rendezVous.DateDebut == default)
                throw new ArgumentException("La date de début est requise et doit être valide.", nameof(rendezVous.DateDebut));

            if (!string.IsNullOrWhiteSpace(rendezVous.Statut))
                ValidateStatus(rendezVous.Statut);
        }

        private void ValidateStatus(string status)
        {
            var validStatuses = new[] { RendezVousStatus.EnAttente, RendezVousStatus.Termine, RendezVousStatus.Annule };
            if (!validStatuses.Contains(status))
                throw new ArgumentException($"Le statut doit être l'un de : {string.Join(", ", validStatuses)}", nameof(status));
        }
    }
}
