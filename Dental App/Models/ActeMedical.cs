using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class ActeMedical
{
    public int Id { get; set; }

    public string Libelle { get; set; } = null!;

    public decimal Prix { get; set; }

    public virtual ICollection<Consultation> IdConsuls { get; set; } = new List<Consultation>();
}
