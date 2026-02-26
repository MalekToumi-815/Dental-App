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
            RunPatientServiceTests();
        }

        // Tests for PatientService: update an existing patient, then create a new patient but do NOT delete it
        private void RunPatientServiceTests()
        {
            try
            {
                var svc = new PatientService(_db);

                string FormatPatient(Patient p)
                {
                    if (p == null) return "(null)";
                    return $"Id={p.Id}, Nom={p.Nom}, Prenom={p.Prenom}, DateNaissance={p.DateNaissance:yyyy-MM-dd}, Sexe={p.Sexe ?? ""}, Telephone={p.Telephone}, SommePaye={p.SommePaye?.ToString() ?? ""}, Adresse={p.Adresse}, Profession={p.Profession ?? ""}, CIN={p.Cin ?? ""}";
                }

                // 1) GetAll and pick an existing patient to update
                var all = svc.GetAllAsync().GetAwaiter().GetResult();
                var sbAll = new StringBuilder();
                sbAll.AppendLine("=== GetAll ===");
                sbAll.AppendLine($"Count: {all?.Count ?? 0}");
                if (all != null && all.Count > 0)
                {
                    foreach (var p in all)
                        sbAll.AppendLine(FormatPatient(p));
                }
                MessageBox.Show(sbAll.ToString(), "PatientService - GetAll", MessageBoxButton.OK, MessageBoxImage.Information);

                if (all == null || all.Count == 0)
                {
                    MessageBox.Show("Aucun patient existant pour tester la mise à jour.", "PatientService", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Select first existing patient for update test
                var existing = all[0];

                // Show selected existing patient
                MessageBox.Show($"=== Selected existing patient to UPDATE ===\n{FormatPatient(existing)}", "PatientService - Selected", MessageBoxButton.OK, MessageBoxImage.Information);

                // 2) Update the existing patient (modify prenom and telephone)
                var originalPrenom = existing.Prenom;
                existing.Prenom = originalPrenom + "_UPDATED";
                existing.Telephone = string.IsNullOrWhiteSpace(existing.Telephone) ? "99999999" : existing.Telephone + "1";

                var updatedExisting = svc.UpdateAsync(existing).GetAwaiter().GetResult();
                MessageBox.Show($"=== Update existing patient ===\n{FormatPatient(updatedExisting)}", "PatientService - Update Existing", MessageBoxButton.OK, MessageBoxImage.Information);

                // Note: We will not revert the change in this test — update is persisted.

                // 3) Create a new patient (do NOT delete it)
                var newPatient = new Patient
                {
                    Nom = "PersistentTempPatient",
                    Prenom = "PTP",
                    DateNaissance = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
                    Telephone = "00000001",
                    Adresse = "persistent address",
                    SommePaye = 0m,
                    Profession = null,
                    Cin = null
                };

                var created = svc.CreateAsync(newPatient).GetAwaiter().GetResult();
                MessageBox.Show($"=== Created new patient (NOT deleted) ===\n{FormatPatient(created)}", "PatientService - Create Persistent", MessageBoxButton.OK, MessageBoxImage.Information);

                // 4) Final count after create
                var finalCount = svc.CountAsync().GetAwaiter().GetResult();
                MessageBox.Show($"=== Final Count ===\nPatients count after create: {finalCount}", "PatientService - Count", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PatientService tests error: {ex.Message}\n{ex.InnerException?.Message}", "PatientService Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}