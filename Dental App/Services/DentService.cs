using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services       
{
    public interface IDentService
    {
        Task InitializeTeethForPatientAsync(int patientId);
        Task<List<Dent>> GetTeethByPatientIdAsync(int patientId);
        Task<Dent?> GetDentByPatientAndFdiAsync(int patientId, int fdiCode);
        Task<List<ToothActHistoryDto>> GetActesByPatientAndFdiAsync(int patientId, int fdiCode);
    }

    public class DentService : IDentService
    {
        private readonly DentalContext _context;

        public DentService(DentalContext context)
        {   
            _context = context;
        }

        /// <summary>
        /// Generates the 32 standard adult teeth (FDI coding system) for a new patient.
        /// Quadrants 1-4, teeth 1-8 per quadrant.
        /// FDI Code: (Quadrant * 10) + ToothNumber
        /// </summary>
        public async Task InitializeTeethForPatientAsync(int patientId)
        {
            try
            {
                // Check if patient exists
                var patient = await _context.Patients.FindAsync(patientId);
                if (patient == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Patient with ID {patientId} not found.");
                    return;
                }

                // Check if teeth already initialized for this patient
                var existingTeeth = await _context.Dents.Where(d => d.PatientId == patientId).CountAsync();
                if (existingTeeth > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Teeth already initialized for patient {patientId}. Skipping initialization.");
                    return;
                }

                var teeth = new List<Dent>();

                // Quadrants 1-4 (using FDI tooth numbering system)
                for (int quad = 1; quad <= 4; quad++)
                {
                    for (int num = 1; num <= 8; num++)
                    {
                        teeth.Add(new Dent { PatientId = patientId, CodeFdi = (quad * 10) + num });
                    }
                }

                // CHILD TEETH (Quadrants 5-8, Teeth 1-5)
                for (int quad = 5; quad <= 8; quad++)
                {
                    for (int num = 1; num <= 5; num++)
                    {
                        teeth.Add(new Dent { PatientId = patientId, CodeFdi = (quad * 10) + num });
                    }
                }

                await _context.Dents.AddRangeAsync(teeth);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✓ Initialized {teeth.Count} teeth for patient {patientId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing teeth for patient: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all teeth for a specific patient, ordered by FDI code.
        /// </summary>
        public async Task<List<Dent>> GetTeethByPatientIdAsync(int patientId)
        {
            try
            {
                return await _context.Dents
                    .Where(d => d.PatientId == patientId)
                    .OrderBy(d => d.CodeFdi)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving teeth for patient: {ex.Message}");
                return new List<Dent>();
            }
        }

        /// <summary>
        /// Get a specific dent by patient id and FDI code.
        /// </summary>
        public async Task<Dent?> GetDentByPatientAndFdiAsync(int patientId, int fdiCode)
        {
            try
            {
                return await _context.Dents
                    .FirstOrDefaultAsync(d => d.PatientId == patientId && d.CodeFdi == fdiCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving dent: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all acts performed on a specific tooth for a patient, with consultation dates
        /// </summary>
        /// <param name="patientId">The patient ID</param>
        /// <param name="fdiCode">The FDI code of the tooth</param>
        /// <returns>List of acts with their consultation dates</returns>
        public async Task<List<ToothActHistoryDto>> GetActesByPatientAndFdiAsync(int patientId, int fdiCode)
        {
            if (patientId <= 0)
                throw new ArgumentException("Le PatientId doit être supérieur à 0.", nameof(patientId));

            if (fdiCode <= 0)
                throw new ArgumentException("Le code FDI doit être supérieur à 0.", nameof(fdiCode));

            try
            {
                // Find the dent for this patient with the specified FDI code
                var dent = await _context.Dents
                    .FirstOrDefaultAsync(d => d.CodeFdi == fdiCode && d.PatientId == patientId);

                if (dent == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DentService] Dent non trouvée pour PatientId={patientId}, FDI={fdiCode}");
                    return new List<ToothActHistoryDto>();
                }

                // Get all consultations for this dent, including their acts
                var consultations = await _context.Consultations
                    .Where(c => c.PatientId == patientId && c.IdDent == dent.Id)
                    .Include(c => c.IdActes)
                    .OrderByDescending(c => c.DateConsultation)
                    .ToListAsync();

                // Build the result DTOs
                var result = new List<ToothActHistoryDto>();

                foreach (var consultation in consultations)
                {
                    var actes = consultation.IdActes?.Select(a => new ActeDto
                    {
                        Id = a.Id,
                        Libelle = a.Libelle
                    }).ToList() ?? new List<ActeDto>();

                    result.Add(new ToothActHistoryDto
                    {
                        ConsultationId = consultation.Id,
                        ConsultationDate = consultation.DateConsultation ?? DateTime.Now,
                        FdiCode = fdiCode,
                        Actes = actes,
                        Notes = consultation.Note
                    });
                }

                System.Diagnostics.Debug.WriteLine($"[DentService] Found {result.Count} consultations for PatientId={patientId}, FDI={fdiCode}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DentService] Error getting acts by patient and FDI: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// DTO for act history on a specific tooth
    /// </summary>
    public class ToothActHistoryDto
    {
        public int ConsultationId { get; set; }
        public DateTime ConsultationDate { get; set; }
        public int FdiCode { get; set; }
        public List<ActeDto> Actes { get; set; } = new List<ActeDto>();
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for medical act information
    /// </summary>
    public class ActeDto
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = null!;
    }
}