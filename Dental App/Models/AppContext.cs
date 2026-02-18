using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dental_App.Models;

public partial class AppContext : DbContext
{
        public AppContext()
    {
    }

    public AppContext(DbContextOptions<AppContext> options)
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=app.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            v => v.ToString("yyyy-MM-dd"),
            v => DateOnly.Parse(v));

        modelBuilder.Entity<ActeMedical>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ActeMedi__3214EC075F8B031D");

            entity.ToTable("ActeMedical");

            entity.Property(e => e.Libelle).HasMaxLength(255);
            entity.Property(e => e.Prix).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Antecedant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Anteceda__3214EC07B461AA38");

            entity.ToTable("Antecedant");

            entity.Property(e => e.Nom).HasMaxLength(255);
        });

        modelBuilder.Entity<Caisse>(entity =>
        {
            entity.HasKey(e => e.DateDuJour).HasName("PK__Caisse__60CDBF3EC933E1D1");

            entity.ToTable("Caisse");

            entity.Property(e => e.DateDuJour)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT");

            entity.Property(e => e.Montant)
                .HasDefaultValue(0m)
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
            entity.HasKey(e => e.Id).HasName("PK__Consulta__3214EC070B62EC18");

            entity.ToTable("Consultation");

            entity.HasIndex(e => e.IdDent, "IX_Consultation_IdDent");

            entity.HasIndex(e => e.PatientId, "IX_Consultation_PatientId");

            entity.Property(e => e.DateConsultation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MontantTotal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdDentNavigation).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.IdDent)
                .HasConstraintName("FK_Consultation_Dent");

            entity.HasOne(d => d.Patient).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Consultation_Patient");

            entity.HasMany(d => d.IdActes).WithMany(p => p.IdConsuls)
                .UsingEntity<Dictionary<string, object>>(
                    "ActeConsultation",
                    r => r.HasOne<ActeMedical>().WithMany()
                        .HasForeignKey("IdActe")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ActeConsul_Acte"),
                    l => l.HasOne<Consultation>().WithMany()
                        .HasForeignKey("IdConsul")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ActeConsul_Consul"),
                    j =>
                    {
                        j.HasKey("IdConsul", "IdActe").HasName("PK__ActeCons__205BE8A7DAD9A0EC");
                        j.ToTable("ActeConsultation");
                    });
        });

        modelBuilder.Entity<Dent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Dent__3214EC07A95B2285");

            entity.ToTable("Dent");

            entity.Property(e => e.CodeFdi).HasColumnName("CodeFDI");
        });

        modelBuilder.Entity<Medicament>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Medicame__3214EC0705259ACA");

            entity.ToTable("Medicament");

            entity.Property(e => e.Nom).HasMaxLength(255);

            entity.HasIndex(e => e.OrdonnanceId, "IX_Medicament_OrdonnanceId");

            entity.HasOne(d => d.Ordonnance).WithMany(p => p.Medicaments)
                .HasForeignKey(d => d.OrdonnanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Medicament_Ordonnance");
        });

        modelBuilder.Entity<OdontogrammeLibre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Odontogr__3214EC078C2999DB");

            entity.ToTable("OdontogrammeLibre");

            entity.HasIndex(e => e.PatientId, "IX_OdontogrammeLibre_PatientId");

            entity.HasOne(d => d.Patient).WithMany(p => p.OdontogrammeLibres)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Odontogramme_Patient");
        });

        modelBuilder.Entity<Ordonnance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ordonnan__3214EC0769A52545");

            entity.ToTable("Ordonnance");

            entity.HasIndex(e => e.PatientId, "IX_Ordonnance_PatientId");

            entity.Property(e => e.DateCreation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Patient).WithMany(p => p.Ordonnances)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ordonnance_Patient");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Patient__3214EC078A5281F7");

            entity.ToTable("Patient");

            entity.Property(e => e.Cin)
                .HasMaxLength(50)
                .HasColumnName("CIN");
            entity.Property(e => e.Nom).HasMaxLength(100);
            entity.Property(e => e.Prenom).HasMaxLength(100);
            entity.Property(e => e.Profession).HasMaxLength(100);
            entity.Property(e => e.Sexe).HasMaxLength(10);
            entity.Property(e => e.SommePaye)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Telephone).HasMaxLength(20);

            entity.Property(e => e.DateNaissance)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT");

            entity.HasMany(d => d.IdAntecedants).WithMany(p => p.IdPatients)
                .UsingEntity<Dictionary<string, object>>(
                    "PatientAntecedant",
                    r => r.HasOne<Antecedant>().WithMany()
                        .HasForeignKey("IdAntecedant")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PatAnt_Ant"),
                    l => l.HasOne<Patient>().WithMany()
                        .HasForeignKey("IdPatient")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PatAnt_Patient"),
                    j =>
                    {
                        j.HasKey("IdPatient", "IdAntecedant").HasName("PK__PatientA__798C43F908F64248");
                        j.ToTable("PatientAntecedant");
                    });
        });

        modelBuilder.Entity<Prothesiste>(entity =>
        {
            entity.ToTable("Prothesiste");
        });

        modelBuilder.Entity<RadioImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RadioIma__3214EC07261052E2");

            entity.ToTable("RadioImage");

            entity.Property(e => e.DatePrise)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Patient).WithMany(p => p.RadioImages)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RadioImage_Patient");
        });

        modelBuilder.Entity<RendezVou>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RendezVo__3214EC07CC1B80E1");

            entity.HasIndex(e => e.PatientId, "IX_RendezVous_PatientId");

            entity.Property(e => e.DateDebut).HasColumnType("datetime");
            entity.Property(e => e.DateFin).HasColumnType("datetime");
            entity.Property(e => e.Statut).HasMaxLength(50);

            entity.HasOne(d => d.Patient).WithMany(p => p.RendezVous)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RendezVous_Patient");
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC0709A8F6B9");

            entity.ToTable("Utilisateur");

            entity.Property(e => e.Nom).HasMaxLength(100);
            entity.Property(e => e.Prenom).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
