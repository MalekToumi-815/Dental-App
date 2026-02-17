using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Ordonnance
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public DateTime? DateCreation { get; set; }

    public virtual ICollection<Medicament> Medicaments { get; set; } = new List<Medicament>();

    public virtual Patient Patient { get; set; } = null!;
}
