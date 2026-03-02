using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class RendezVou
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public DateTime DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public string? Statut { get; set; } // "en attente", "termine", "annule"

    public virtual Patient Patient { get; set; } = null!;
}
