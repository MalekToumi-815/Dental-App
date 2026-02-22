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
            // Read the first row in Utilisateur and display it
            var user = System.Linq.Enumerable.FirstOrDefault(_db.Utilisateurs);
            if (user != null)
            {
                System.Diagnostics.Debug.WriteLine($"Utilisateur: Id={user.Id}, Nom={user.Nom}, Prenom={user.Prenom}, Hash={user.MotDePasseHash}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No utilisateur found.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
        }
    }
}