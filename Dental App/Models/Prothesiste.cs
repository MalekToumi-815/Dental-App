using System;
using System.Collections.Generic;

namespace Dental_App.Models;

public partial class Prothesiste
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string? Adresse { get; set; }

    public string? Tel { get; set; }

    public virtual ICollection<CommandeProthesiste> CommandeProthesistes { get; set; } = new List<CommandeProthesiste>();
}
