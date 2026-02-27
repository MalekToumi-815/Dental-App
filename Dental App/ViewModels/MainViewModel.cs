using Dental_App.Models;
using Dental_App.Services;
using Microsoft.EntityFrameworkCore;
using Prism.Mvvm;
using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace Dental_App.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly DentalContext _db;

        public MainViewModel(DentalContext db)
        {
            _db = db;
            RunAntecedentServiceTests();
        }

        // Tests for AntecedentService: run each method and show clear output after each
        private void RunAntecedentServiceTests()
        {
            try
            {
                var svc = new AntecedentService(_db);
                svc.AjouterAntecedantAsync(2 ,1).GetAwaiter().GetResult(); // just to ensure the service is initialized and can access the DB
                // Helper to format actual Antecedant model
                /*string FormatAntecedant(Antecedant a)
                {
                    if (a == null) return "(null)";
                    return $"Id={a.Id}, Nom={a.Nom}, Description={a.Description ?? ""}";
                }

                // 1) GetAll
                var all = svc.GetAllAsync().GetAwaiter().GetResult();
                var sbAll = new StringBuilder();
                sbAll.AppendLine("=== Antecedant - GetAll ===");
                sbAll.AppendLine($"Count: {all?.Count ?? 0}");
                if (all != null && all.Count > 0)
                {
                    foreach (var a in all)
                        sbAll.AppendLine(FormatAntecedant(a));
                }
                MessageBox.Show(sbAll.ToString(), "AntecedantService - GetAll", MessageBoxButton.OK, MessageBoxImage.Information);

                // 2) Create
                var uniqueName = "TempAntTest_" + DateTime.Now.Ticks;
                var toCreate = new Antecedant
                {
                    Nom = uniqueName,
                    Description = "Test description"
                };

                var created = svc.CreateAsync(toCreate).GetAwaiter().GetResult();
                MessageBox.Show($"=== Create ===\n{FormatAntecedant(created)}", "AntecedantService - Create", MessageBoxButton.OK, MessageBoxImage.Information);

                // 3) GetById
                var byId = svc.GetByIdAsync(created.Id).GetAwaiter().GetResult();
                MessageBox.Show($"=== GetById ({created.Id}) ===\n{FormatAntecedant(byId)}", "AntecedantService - GetById", MessageBoxButton.OK, MessageBoxImage.Information);

                // 4) GetByName
                var byNameList = svc.GetByNameAsync(created.Nom).GetAwaiter().GetResult();
                var sbByName = new StringBuilder();
                sbByName.AppendLine($"=== GetByName ('{created.Nom}') ===");
                sbByName.AppendLine($"Results: {byNameList.Count}");
                foreach (var a in byNameList)
                    sbByName.AppendLine(FormatAntecedant(a));
                MessageBox.Show(sbByName.ToString(), "AntecedantService - GetByName", MessageBoxButton.OK, MessageBoxImage.Information);

                // 5) Update
                created.Description = "Updated description";
                var updated = svc.UpdateAsync(created).GetAwaiter().GetResult();
                MessageBox.Show($"=== Update ===\n{FormatAntecedant(updated)}", "AntecedantService - Update", MessageBoxButton.OK, MessageBoxImage.Information);

                // 6) Exists
                var exists = svc.ExistsAsync(created.Nom).GetAwaiter().GetResult();
                MessageBox.Show($"=== Exists ===\nNom: {created.Nom}\nExists: {exists}", "AntecedantService - Exists", MessageBoxButton.OK, MessageBoxImage.Information);

                // 7) Count
                var count = svc.CountAsync().GetAwaiter().GetResult();
                MessageBox.Show($"=== Count ===\nTotal antecedants: {count}", "AntecedantService - Count", MessageBoxButton.OK, MessageBoxImage.Information);

                // 8) DeleteByName
                var deletedByName = svc.DeleteByNameAsync(created.Nom).GetAwaiter().GetResult();
                MessageBox.Show($"=== DeleteByName ===\nRequested name: {created.Nom}\nDeleted rows: {deletedByName}", "AntecedantService - DeleteByName", MessageBoxButton.OK, MessageBoxImage.Information);

                // 9) Delete (attempt to delete by id) - should be false because already deleted
                var deletedById = svc.DeleteAsync(created.Id).GetAwaiter().GetResult();
                MessageBox.Show($"=== Delete by Id ({created.Id}) ===\nDeleted: {deletedById}", "AntecedantService - Delete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Final list
                var finalList = svc.GetAllAsync().GetAwaiter().GetResult();
                var sbFinal = new StringBuilder();
                sbFinal.AppendLine("=== Final antecedants ===");
                sbFinal.AppendLine($"Count: {finalList.Count}");
                foreach (var a in finalList)
                    sbFinal.AppendLine(FormatAntecedant(a));
                MessageBox.Show(sbFinal.ToString(), "AntecedantService - Final", MessageBoxButton.OK, MessageBoxImage.Information);
            */}
            catch (Exception ex)
            {
                MessageBox.Show($"AntecedantService tests error: {ex.Message}\n{ex.InnerException?.Message}", "AntecedantService Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}