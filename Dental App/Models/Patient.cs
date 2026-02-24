using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Patient
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public DateOnly DateNaissance { get; set; } 

    public string? Sexe { get; set; }

    public string Telephone { get; set; } = null!;

    public decimal? SommePaye { get; set; }

    public string Adresse { get; set; } = null!;

    public string? Profession { get; set; }

    public string? Cin { get; set; }

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();

    public virtual ICollection<OdontogrammeLibre> OdontogrammeLibres { get; set; } = new List<OdontogrammeLibre>();

    public virtual ICollection<Ordonnance> Ordonnances { get; set; } = new List<Ordonnance>();

    public virtual ICollection<RadioImage> RadioImages { get; set; } = new List<RadioImage>();

    public virtual ICollection<RendezVou> RendezVous { get; set; } = new List<RendezVou>();

    public virtual ICollection<Antecedant> IdAntecedants { get; set; } = new List<Antecedant>();
}
