using System.IO;
using Microsoft.EntityFrameworkCore;
using Dental_App.Models;

public interface IRadioImageService
{
    Task<RadioImage> ImportRadioImageAsync(int patientId, string sourcePath, string type, DateTime datePrise);
    Task<List<RadioImage>> GetPatientImagesAsync(int patientId);
    string GetFullPath(int patientId, string fileName);
    Task DeleteImageAsync(int id);
}

public class RadioImageService : IRadioImageService
{
    private readonly DentalContext _context;
    private readonly string _appDataRoot;

    public RadioImageService(DentalContext context)
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

    public async Task<RadioImage> ImportRadioImageAsync(int patientId, string sourcePath, string type, DateTime datePrise)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source image not found.");

        // 1. Prepare the destination folder: Documents/DentalApp_Data/Patients/{ID}/Radio
        string relativeFolder = Path.Combine("Patients", patientId.ToString(), "Radio");
        string absoluteFolder = Path.Combine(_appDataRoot, relativeFolder);

        if (!Directory.Exists(absoluteFolder))
            Directory.CreateDirectory(absoluteFolder);

        // 2. Generate a unique filename to prevent overwriting 
        // Extract the file extension from the source file
        string extension = Path.GetExtension(sourcePath);
        // Example: 20231025_143005_type.jpg
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string uniqueFileName = $"{timestamp}_{type}{extension}";
        string destinationPath = Path.Combine(absoluteFolder, uniqueFileName);

        // 3. Copy the file to our managed storage
        File.Copy(sourcePath, destinationPath, overwrite: true);

        // 4. Save the metadata to the database
        var radioRecord = new RadioImage
        {
            PatientId = patientId,
            FileName = uniqueFileName, // Store only the filename
            Type = type,
            DatePrise = datePrise
        };

        _context.RadioImages.Add(radioRecord);
        await _context.SaveChangesAsync();

        return radioRecord;
    }

    public async Task<List<RadioImage>> GetPatientImagesAsync(int patientId)
    {
        return await _context.RadioImages
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.DatePrise)
            .ToListAsync();
    }

    public string GetFullPath(int patientId, string fileName)
    {
        return Path.Combine(_appDataRoot, "Patients", patientId.ToString(), "Radio", fileName);
    }

    public async Task DeleteImageAsync(int id)
    {
        var record = await _context.RadioImages.FindAsync(id);
        if (record != null)
        {
            // Delete physical file first
            string path = GetFullPath(record.PatientId, record.FileName!);
            if (File.Exists(path)) File.Delete(path);

            // Delete DB record
            _context.RadioImages.Remove(record);
            await _context.SaveChangesAsync();
        }
    }
}