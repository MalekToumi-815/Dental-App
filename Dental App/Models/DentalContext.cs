using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.IO;

namespace Dental_App.Models;

public partial class DentalContext : DbContext
{
    public DentalContext()
    {
    }

    public DentalContext(DbContextOptions<DentalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActeMedical> ActeMedicals { get; set; }

    public virtual DbSet<Antecedant> Antecedants { get; set; }

    public virtual DbSet<Caisse> Caisses { get; set; }

    public virtual DbSet<CommandeProthesiste> CommandeProthesistes { get; set; }

    public virtual DbSet<Consultation> Consultations { get; set; }

    public virtual DbSet<Dent> Dents { get; set; }

    public virtual DbSet<Medicament> Medicaments { get; set; }

    public virtual DbSet<OdontogrammeLibre> OdontogrammeLibres { get; set; }

    public virtual DbSet<Ordonnance> Ordonnances { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Prothesiste> Prothesistes { get; set; }

    public virtual DbSet<RadioImage> RadioImages { get; set; }

    public virtual DbSet<RendezVou> RendezVous { get; set; }

    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // converter for DateOnly <-> TEXT (ISO yyyy-MM-dd)
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            v => v.ToString("yyyy-MM-dd"),
            v => DateOnly.Parse(v));

        modelBuilder.Entity<ActeMedical>(entity =>
        {
            entity.ToTable("ActeMedical");
        });

        modelBuilder.Entity<Antecedant>(entity =>
        {
            entity.ToTable("Antecedant");

            entity.Property(e => e.PatientId).HasColumnName("PatientId");

            entity.HasIndex(e => e.PatientId, "IX_Antecedant_PatientId");

            entity.HasOne(d => d.Patient).WithMany(p => p.Antecedants)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Antecedant_Patient");
        });

        modelBuilder.Entity<Caisse>(entity =>
        {
            entity.HasKey(e => e.DateDuJour);

            entity.ToTable("Caisse");

            // ensure DateOnly is converted to TEXT for SQLite
            entity.Property(e => e.DateDuJour)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT");

            entity.Property(e => e.Montant)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<CommandeProthesiste>(entity =>
        {
            entity.ToTable("Commande_Prothesiste");

            entity.Property(e => e.Date)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME");
            entity.Property(e => e.IdProthesiste).HasColumnName("Id_Prothesiste");
            entity.Property(e => e.SommePayees)
                .HasDefaultValue(0.0)
                .HasColumnName("Somme_Payees");

            entity.HasOne(d => d.IdProthesisteNavigation).WithMany(p => p.CommandeProthesistes)
                .HasForeignKey(d => d.IdProthesiste)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.ToTable("Consultation");

            entity.HasIndex(e => e.IdDent, "IX_Consultation_IdDent");

            entity.HasIndex(e => e.PatientId, "IX_Consultation_PatientId");

            entity.Property(e => e.DateConsultation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MontantTotal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdDentNavigation).WithMany(p => p.Consultations).HasForeignKey(d => d.IdDent);

            entity.HasOne(d => d.Patient).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(d => d.IdActes).WithMany(p => p.IdConsuls)
                .UsingEntity<Dictionary<string, object>>(
                    "ActeConsultation",
                    r => r.HasOne<ActeMedical>().WithMany()
                        .HasForeignKey("IdActe")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    l => l.HasOne<Consultation>().WithMany()
                        .HasForeignKey("IdConsul")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    j =>
                    {
                        j.HasKey("IdConsul", "IdActe");
                        j.ToTable("ActeConsultation");
                        j.HasIndex(new[] { "IdActe" }, "IX_ActeConsultation_IdActe");
                    });
        });

        modelBuilder.Entity<Dent>(entity =>
        {
            entity.ToTable("Dent");

            entity.Property(e => e.CodeFdi).HasColumnName("CodeFDI");

            // index on PatientId for lookup
            entity.HasIndex(e => e.PatientId, "IX_Dent_PatientId");

            // relationship: Dent -> Patient (optional)
            entity.HasOne(d => d.Patient).WithMany(p => p.Dents)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dent_Patient");
        });

        modelBuilder.Entity<Medicament>(entity =>
        {
            entity.ToTable("Medicament");

            entity.HasIndex(e => e.OrdonnanceId, "IX_Medicament_OrdonnanceId");

            entity.HasOne(d => d.Ordonnance).WithMany(p => p.Medicaments)
                .HasForeignKey(d => d.OrdonnanceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OdontogrammeLibre>(entity =>
        {
            entity.ToTable("OdontogrammeLibre");

            entity.HasIndex(e => e.PatientId, "IX_OdontogrammeLibre_PatientId");

            entity.HasOne(d => d.Patient).WithMany(p => p.OdontogrammeLibres)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Ordonnance>(entity =>
        {
            entity.ToTable("Ordonnance");

            entity.HasIndex(e => e.PatientId, "IX_Ordonnance_PatientId");

            entity.Property(e => e.DateCreation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Patient).WithMany(p => p.Ordonnances)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patient");

            // map DateOnly <-> TEXT
            entity.Property(e => e.DateNaissance)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT");

            entity.Property(e => e.Cin).HasColumnName("CIN");
            entity.Property(e => e.SommePaye)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Prothesiste>(entity =>
        {
            entity.ToTable("Prothesiste");
        });

        modelBuilder.Entity<RadioImage>(entity =>
        {
            entity.ToTable("RadioImage");

            entity.HasIndex(e => e.PatientId, "IX_RadioImage_PatientId");

            entity.Property(e => e.DatePrise)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Patient).WithMany(p => p.RadioImages)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RendezVou>(entity =>
        {
            entity.HasIndex(e => e.PatientId, "IX_RendezVous_PatientId");

            entity.Property(e => e.DateDebut).HasColumnType("datetime");

            entity.HasOne(d => d.Patient).WithMany(p => p.RendezVous)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.ToTable("Utilisateur");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
