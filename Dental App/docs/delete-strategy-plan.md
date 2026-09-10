# Delete strategy plan — soft-delete `Patient`, hard-delete other entities

Goal

- Implement a mixed deletion strategy:
  - Soft-delete for `Patient` (add `IsDeleted` boolean) and apply a global EF Core query filter so deleted patients never appear in lists.
  - Hard-delete (physical removal) for entities: `ActeMedical`, `Caisse`, `Consultation`, `RadioImage`, `RendezVou`, `Ordonnance`.

Why this approach

- `Patient` often has many dependents and audit/history value; soft-delete avoids orphaning or losing history and is safe when there are multiple FK relationships.
- Hard-delete for the other entities simplifies storage and cleanup; those rows are typically transactional and easier to remove.

Prerequisites & safety

- Back up the database before running migrations or mass deletes.
- Make changes and migrations on a development copy first, verify, then promote to staging/production.
- Ensure `dotnet-ef` is available for migrations (or use Visual Studio Package Manager Console).

High-level steps (ordered)

1. Add `IsDeleted` to the `Patient` model.
2. Add a global query filter for `Patient` in `DentalContext.OnModelCreating` and configure default value.
3. Create & run EF Core migration to add `IsDeleted` column.
4. Add `SoftDeletePatientAsync` and `RestorePatientAsync` to the patient service API and implement them.
5. Update all patient retrieval service methods and view-models if they bypass the DbContext filter (most won't need change if the global filter is used).
6. Add UI affordances for soft-delete (delete button + confirmation) in `PatientsView` and `PatientsViewModel`.
7. Add delete methods to services for hard-delete entities (interface + implementations): `DeleteActeMedicalAsync`, `DeleteCaisseAsync`, `DeleteConsultationAsync`, `DeleteRadioImageAsync`, `DeleteRendezVousAsync`, `DeleteOrdonnanceAsync`.
8. Wire delete commands and UI (delete button per row) in each corresponding list view and view-model.
9. Handle dependent relationships before deletion (clear many-to-many links or delete children explicitly) and use transactions for multi-step deletes.
10. Add unit/integration tests and manual test plan; run tests locally.
11. Add logging and user notifications for deletes.
12. Deploy migration, test on staging, then production.

Detailed step-by-step plan

Step 0 — Preparation

- Make a DB backup.
- Ensure `dotnet-ef` is installed: `dotnet tool install --global dotnet-ef` (if needed).
- Open solution in Visual Studio or ensure CLI works from solution root.

Step 1 — Model change: add `IsDeleted` to patient

Files to edit

- `Dental App/Models/Patient.cs` (or wherever `Patient` model declared)
- `Dental App/Models/DentalContext.cs` (existing file — you'll add query filter here)

Change to `Patient` model (add a CLR property)

```csharp
// inside class Patient
public bool IsDeleted { get; set; } = false;
```

Step 2 — Add global query filter in `DentalContext`

Edit `OnModelCreating` in `Dental App/Models/DentalContext.cs` and locate the `modelBuilder.Entity<Patient>` block. Add:

```csharp
entity.Property(p => p.IsDeleted).HasDefaultValue(false);
entity.HasQueryFilter(p => !p.IsDeleted);
```

Notes

- Global query filters are applied automatically to LINQ queries through EF Core. Any raw SQL or queries explicitly ignoring filters must be checked.
- If you prefer shadow property instead of CLR property, you can use `entity.Property<bool>("IsDeleted")` and `HasQueryFilter(p => !EF.Property<bool>(p, "IsDeleted"))` but a CLR boolean property is simpler.

Step 3 — Migration

From solution root (example CLI commands):

```bash
# create migration
dotnet ef migrations add AddPatientIsDeleted --project "Dental App" --startup-project "Dental App"

# apply migration to DB
dotnet ef database update --project "Dental App" --startup-project "Dental App"
```

Or in Visual Studio Package Manager Console:

```
Add-Migration AddPatientIsDeleted -Project "Dental App" -StartupProject "Dental App"
Update-Database -Project "Dental App" -StartupProject "Dental App"
```

Step 4 — Patient service: soft-delete methods

Files to edit / create

- Interface: `Dental App/Services/IPatientService.cs` (if it exists; add methods)
- Implementation: `Dental App/Services/PatientService.cs`

Add to interface

```csharp
Task<bool> SoftDeletePatientAsync(int id);
Task<bool> RestorePatientAsync(int id); // optional
```

Implementation example

```csharp
public async Task<bool> SoftDeletePatientAsync(int id)
{
    var p = await _context.Patients.FindAsync(id);
    if (p == null) return false;
    p.IsDeleted = true;
    _context.Patients.Update(p);
    try
    {
        await _context.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateException)
    {
        // log
        return false;
    }
}

public async Task<bool> RestorePatientAsync(int id)
{
    var p = await _context.Patients.FindAsync(id);
    if (p == null) return false;
    p.IsDeleted = false;
    _context.Patients.Update(p);
    try
    {
        await _context.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateException)
    {
        // log
        return false;
    }
}
```

Step 5 — Update views / view-models that list patients

Check places where patients are listed (for example `PatientsViewModel`). If those views call a patient service method that returns all patients, they will automatically exclude soft-deleted entries once the global filter is in place — no further change required. If any code uses `IgnoreQueryFilters()` or raw SQL, update it.

Files to check

- `Dental App/ViewModels/PatientsViewModel.cs`
- Any other view models referencing `_context.Patients` directly

Step 6 — UI: add delete action for patients

Files to edit

- `Dental App/Views/PatientsView.xaml`
- `Dental App/ViewModels/PatientsViewModel.cs`

Tasks

- Add a `Delete` button in the patient list row template (with tooltip and confirmation dialog).
- Add `DelegateCommand<int> DeletePatientCommand` or similar in `PatientsViewModel` and call `SoftDeletePatientAsync`.
- After deletion, refresh the list and show a notification.

Example XAML snippet (row template)

```xml
<Button Style="{StaticResource IconButtonStyle}" Command="{Binding DataContext.DeletePatientCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}" CommandParameter="{Binding Id}" ToolTip="Supprimer">
    <!-- icon/path -->
</Button>
```

Example ViewModel code

```csharp
DeletePatientCommand = new DelegateCommand<int>(async id =>
{
    var ok = await _patientService.SoftDeletePatientAsync(id);
    if (ok) { Refresh(); notificationService.ShowSuccess("Patient supprimé"); }
    else { notificationService.ShowError("Suppression échouée"); }
});
```

Step 7 — Hard delete methods for other entities

Entities to implement hard delete for:

- `ActeMedical`
- `Caisse`
- `Consultation`
- `RadioImage`
- `RendezVou`
- `Ordonnance`

Files to edit

- Add `DeleteXAsync(int id)` to each service interface (`ICaisseService`, `IConsultationService`, etc.).
- Implement `DeleteXAsync` in each concrete service class.

General implementation template (e.g., `CaisseService`)

```csharp
public async Task<bool> DeleteCaisseAsync(int id)
{
    var entity = await _context.Caisses.FindAsync(id);
    if (entity == null) return false;

    _context.Caisses.Remove(entity);
    try
    {
        await _context.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateException ex)
    {
        // log and return false
        return false;
    }
}
```

Special handling for `Consultation` (many-to-many `ActeConsultation`)

- Load the consultation including its `IdActes` collection and clear the association before deletion to avoid FK problems:

```csharp
var consult = await _context.Consultations
    .Include(c => c.IdActes)
    .FirstOrDefaultAsync(c => c.Id == id);
if (consult == null) return false;
consult.IdActes.Clear();
_context.Consultations.Remove(consult);
await _context.SaveChangesAsync();
```

Step 8 — UI: add delete support for lists

- `CaisseView` (transactions): add a delete icon to each row; add a `DeleteTransactionCommand` in `CaisseViewModel` and call `DeleteCaisseAsync(id)`; refresh list.
- Similarly for `ConsultationView`, `RadioImagesView`, `RendezVousView`, `OrdonnancesView`, and any other list.
- Add confirmation dialog before deleting.

Step 9 — Transactions & safety during deletes

- For deletes that include multiple operations (clear many-to-many then remove), wrap them in a `using var tx = await _context.Database.BeginTransactionAsync()` block and commit after `SaveChangesAsync()`.

Step 10 — Tests

- Unit tests for service delete methods:
  - Deleting non-existent id returns `false`.
  - Deleting entity removes it from DB.
  - Soft-deleting a patient hides it from normal queries.
- Integration test: run migration, create test data, run delete, assert results.

Step 11 — Logging & notifications

- Log attempted deletes and failures.
- Use `_notificationService` to notify the user of success/failure.

Step 12 — Migration & deployment

- Create migration(s) and apply to staging.
- Run functional tests on staging.
- Deploy to production and apply migration.

Edge cases & notes

- Global query filters do not affect raw SQL queries. Search places where `FromSqlRaw` or manual SQL is used.
- If you expect to permanently purge soft-deleted patients later, add a periodic cleanup job.
- Consider adding `DeletedAt` and `DeletedBy` for audit.
- If some FK columns are non-nullable and configured `ClientSetNull`, hard deletes may fail — prefer clearing children first or switch to cascade with careful review.

Backout / rollback plan

- Keep DB backups before applying migration.
- If migration fails, restore the backup.
- For UI changes, deploy behind a feature flag if desired.

Estimated effort per step (rough)

- Model + context change + migration: 1–2 hours
- PatientService methods + tests: 1–2 hours
- Patients UI + viewmodel wiring: 1–2 hours
- Each hard-delete service (one entity): 0.5–1 hour
- Each list UI change and viewmodel wiring: 0.5–1 hour
- Integration tests and QA: 2–4 hours
- Staging & production rollout: 1–2 hours

Acceptance criteria

- `Patient` soft-delete toggles `IsDeleted` and patient no longer appears anywhere in the UI lists.
- Hard-deleting an entity removes it and related join rows with no DB integrity errors.
- All changes are covered by tests and manual validation steps.

Next action

- Tell me which step to execute first. I will implement it and run the build/tests before moving to the next step.

---

Generated for the repository at the solution root. If you want, I can split and create individual PR-style branches per step and implement them one-by-one.