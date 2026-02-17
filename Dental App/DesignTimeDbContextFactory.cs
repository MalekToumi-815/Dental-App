using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentalContext>
{
    public DentalContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DentalContext>();
        optionsBuilder.UseSqlite("Data Source=app.db");

        return new DentalContext(optionsBuilder.Options);
    }
}