using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<Dental_App.Models.DentalContext>
{
    public Dental_App.Models.DentalContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Dental_App.Models.DentalContext>();
        optionsBuilder.UseSqlite("Data Source=app.db");

        return new Dental_App.Models.DentalContext(optionsBuilder.Options);
    }
}