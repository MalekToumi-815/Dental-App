using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Antecedant
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Patient> IdPatients { get; set; } = new List<Patient>();
}
