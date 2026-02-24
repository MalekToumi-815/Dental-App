using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentalContext>
{
    public DentalContext CreateDbContext(string[] args)
    {
        // Same folder as in App.xaml.cs
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Dental_App"
        );

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "dental.db");

        var optionsBuilder = new DbContextOptionsBuilder<DentalContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new DentalContext(optionsBuilder.Options);
    }
}