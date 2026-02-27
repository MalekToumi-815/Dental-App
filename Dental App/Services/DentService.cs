using Dental_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Dental_App.Services       
{
    public interface IDentService
    {
        Task InitializeTeethForPatientAsync(int patientId);
        Task<List<Dent>> GetTeethByPatientIdAsync(int patientId);
        Task<Dent> GetDentByIdAsync(int dentId);
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
                for (int quadrant = 1; quadrant <= 4; quadrant++)
                {
                    // Teeth 1-8 in each quadrant
                    for (int toothNumber = 1; toothNumber <= 8; toothNumber++)
                    {
                        int fdiCode = (quadrant * 10) + toothNumber;

                        teeth.Add(new Dent
                        {
                            PatientId = patientId,
                            CodeFdi = fdiCode
                        });
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
        /// Get a specific dent by ID.
        /// </summary>
        public async Task<Dent> GetDentByIdAsync(int dentId)
        {
            try
            {
                return await _context.Dents.FindAsync(dentId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving dent: {ex.Message}");
                return null;
            }
        }
    }
}
