using System.IO;
using Microsoft.EntityFrameworkCore;
using Dental_App.Models;

public interface IOdontogrammeLibreService
{
    // Gets the record from DB or creates a new one if it doesn't exist
    Task<OdontogrammeLibre> GetOrCreateRecordingAsync(int patientId);

    // Generates the absolute path from a relative path stored in DB
    string GetFullPath(string relativePath);
}
public class OdontogrammeLibreService : IOdontogrammeLibreService
{
    private readonly DentalContext _context;
    private readonly string _appDataRoot;

    public OdontogrammeLibreService(DentalContext context)
    {
        _context = context;

        // 1. Define the Root Path
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _appDataRoot = Path.Combine(documents, "DentalApp_Data");

        // 2. Ensure the Global Root exists immediately
        // If "DentalApp_Data" isn't there, it creates it now.
        if (!Directory.Exists(_appDataRoot))
        {
            Directory.CreateDirectory(_appDataRoot);
        }
    }

    public async Task<OdontogrammeLibre> GetOrCreateRecordingAsync(int patientId)
    {
        // 1. Check if the record already exists in the database
        var record = await _context.OdontogrammeLibres
            .FirstOrDefaultAsync(o => o.PatientId == patientId);

        // 2. If it doesn't exist, we set up the folder and the DB entry
        if (record == null)
        {
            // Define the specific folder for this patient's drawings
            string relativeFolder = Path.Combine("Patients", patientId.ToString(), "Ink");
            string absoluteFolder = Path.Combine(_appDataRoot, relativeFolder);

            // Create the folder on the hard drive if it's missing
            if (!Directory.Exists(absoluteFolder))
            {
                Directory.CreateDirectory(absoluteFolder);
            }

            // We store the RELATIVE path in the DB so the app stays portable
            string relativeFileName = Path.Combine(relativeFolder, "teeth_drawing.isf");

            record = new OdontogrammeLibre
            {
                PatientId = patientId,
                InkFilePath = relativeFileName
            };

            _context.OdontogrammeLibres.Add(record);
            await _context.SaveChangesAsync();
        }

        return record;
    }

    /// <summary>
    /// Call this when you actually need to Load or Save the file in the UI.
    /// It combines the root path with the relative path stored in the DB.
    /// </summary>
    public string GetFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return string.Empty;
        return Path.Combine(_appDataRoot, relativePath);
    }
}