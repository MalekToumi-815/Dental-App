using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Medicament
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string? Posologie { get; set; }

    public int OrdonnanceId { get; set; }

    public virtual Ordonnance Ordonnance { get; set; } = null!;
}
