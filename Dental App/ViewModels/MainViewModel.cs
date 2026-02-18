using Dental_App.Models;

public class MainViewModel : BindableBase
{
    private readonly Dental_App.Models.AppContext _db;

    public MainViewModel(Dental_App.Models.AppContext db)
    {
        _db = db;
        CheckDatabase();
    }

    private void CheckDatabase()
    {
        try
        {
            // Try to count rows in one of your tables (e.g., Patients)
            var count = _db.Patients.Count();
            System.Diagnostics.Debug.WriteLine($"Database connected! Row count: {count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
        }
    }
}