using Dental_App.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class MainViewModel : BindableBase
{
    private readonly Dental_App.Models.DentalContext _db;
    
    public MainViewModel(Dental_App.Models.DentalContext db)
    {
        _db = db;
        CheckDatabase();
    }

    private void CheckDatabase()
    {
        try
        {
            var service = new CaisseService(_db);
            var added = Task.Run(() => service.AddMontantAsync(100m)).GetAwaiter().GetResult();

            if (!added)
            {
                System.Diagnostics.Debug.WriteLine("Failed to add montant to today's caisse.");
                return;
            }

            var today = DateOnly.FromDateTime(System.DateTime.Now);
            var caisse = Task.Run(() => service.GetCaisseByDateAsync(today)).GetAwaiter().GetResult();

            if (caisse != null)
            {
                System.Diagnostics.Debug.WriteLine($"✓ Caisse today: Date={caisse.DateDuJour:yyyy-MM-dd}, Montant={caisse.Montant}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Caisse not found after insertion.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("FULL ERROR:");
            System.Diagnostics.Debug.WriteLine(ex.ToString());

            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine("INNER EXCEPTION:");
                System.Diagnostics.Debug.WriteLine(ex.InnerException.ToString());
            }
        }
    }
}